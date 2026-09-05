using FluentAssertions;
using RMS.Application.Features.Reporting.DTOs;
using RMS.Application.Features.Reporting.Services;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Services;

/// <summary>
/// Feature 7 (US-022/US-025): pure calculation tests, no DB/mocks needed since ReportingCalculations
/// takes already-loaded entities directly - same style as ProcurementServiceTests.
/// </summary>
public class ReportingCalculationsTests
{
    private static RequisitionItem NewItem(string name, int quantity) => new() { ItemName = name, Quantity = quantity };

    private static Requisition NewRequisition(RequisitionStatus status, RequisitionCategory? category = null, params RequisitionItem[] items)
    {
        var requisition = new Requisition
        {
            Status = status,
            Category = category ?? new RequisitionCategory { Name = "IT" },
            RequestedByUser = new User { FullName = "Test User", Department = "IT" },
        };
        requisition.Items.AddRange(items);
        return requisition;
    }

    private static void AddHistory(Requisition r, RequisitionStatus? from, RequisitionStatus to, DateTime atUtc) =>
        r.StatusHistory.Add(new RequisitionStatusHistory { FromStatus = from, ToStatus = to, CreatedAtUtc = atUtc, ActorName = "Test" });

    [Fact]
    public void BuildItemsSummary_MultipleItems_FormatsEachWithQuantity()
    {
        var requisition = NewRequisition(RequisitionStatus.Approved, items: [NewItem("Laptop", 2), NewItem("Mouse", 1)]);

        var summary = ReportingCalculations.BuildItemsSummary(requisition);

        summary.Should().Be("Laptop x2, Mouse x1");
    }

    [Theory]
    [InlineData(RequisitionStatus.Draft, "Draft")]
    [InlineData(RequisitionStatus.Submitted, "Awaiting Review")]
    [InlineData(RequisitionStatus.Rejected, "Rejected")]
    [InlineData(RequisitionStatus.PartiallyApproved, "Partially Approved")]
    [InlineData(RequisitionStatus.SentBack, "Sent Back for Correction")]
    [InlineData(RequisitionStatus.Cancelled, "Cancelled")]
    [InlineData(RequisitionStatus.Approved, "Fully Approved")]
    [InlineData(RequisitionStatus.Fulfilled, "Fully Approved")]
    public void DescribeApprovalStatus_MapsEachStatusToAHumanLabel(RequisitionStatus status, string expected)
    {
        var requisition = NewRequisition(status);

        ReportingCalculations.DescribeApprovalStatus(requisition).Should().Be(expected);
    }

    [Fact]
    public void DescribeApprovalStatus_UnderReview_NamesThePendingStage()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview);
        requisition.ApprovalProcess = new RequisitionApprovalProcess
        {
            StageInstances =
            {
                new RequisitionApproval
                {
                    Status = RequisitionApprovalStatus.Pending,
                    ApprovalWorkflowStage = new ApprovalWorkflowStage { Name = "HR Review" },
                },
            },
        };

        ReportingCalculations.DescribeApprovalStatus(requisition).Should().Be("Pending: HR Review");
    }

    [Theory]
    [InlineData(RequisitionStatus.UnderReview, null)]
    [InlineData(RequisitionStatus.Submitted, null)]
    [InlineData(RequisitionStatus.Approved, "Awaiting Processing")]
    [InlineData(RequisitionStatus.InProcurement, "In Procurement")]
    [InlineData(RequisitionStatus.PartiallyFulfilled, "Partially Fulfilled")]
    [InlineData(RequisitionStatus.Fulfilled, "Fulfilled")]
    [InlineData(RequisitionStatus.Closed, "Closed")]
    public void DescribeProcurementStatus_NullUntilApprovedOrLater(RequisitionStatus status, string? expected)
    {
        var requisition = NewRequisition(status);

        ReportingCalculations.DescribeProcurementStatus(requisition).Should().Be(expected);
    }

    [Fact]
    public void ComputeProcessingDays_NeverSubmitted_ReturnsNull()
    {
        var requisition = NewRequisition(RequisitionStatus.Draft);

        ReportingCalculations.ComputeProcessingDays(requisition).Should().BeNull();
    }

    [Fact]
    public void ComputeProcessingDays_SubmittedButNoDecisionYet_ReturnsNull()
    {
        var requisition = NewRequisition(RequisitionStatus.UnderReview);
        AddHistory(requisition, RequisitionStatus.Draft, RequisitionStatus.Submitted, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        ReportingCalculations.ComputeProcessingDays(requisition).Should().BeNull();
    }

    [Fact]
    public void ComputeProcessingDays_SubmittedThenApproved_ReturnsElapsedDays()
    {
        var requisition = NewRequisition(RequisitionStatus.Approved);
        AddHistory(requisition, RequisitionStatus.Draft, RequisitionStatus.Submitted, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddHistory(requisition, RequisitionStatus.UnderReview, RequisitionStatus.Approved, new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc));

        ReportingCalculations.ComputeProcessingDays(requisition).Should().Be(3.0);
    }

    [Fact]
    public void BuildSummary_ManpowerRequisitionWithMultipleLines_SumsQuantitiesAcrossLines_NotCountedAsOne()
    {
        // MR-001 from the spec: Software Engineer x3 + HR Executive x2 = 5 total positions, not 1.
        var manpowerCategory = new RequisitionCategory { Name = "Manpower" };
        var mr001 = NewRequisition(RequisitionStatus.Approved, manpowerCategory, NewItem("Software Engineer", 3), NewItem("HR Executive", 2));
        AddHistory(mr001, RequisitionStatus.Draft, RequisitionStatus.Submitted, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var summary = ReportingCalculations.BuildSummary([mr001]);

        summary.ManpowerRequisitionCount.Should().Be(1);
        summary.ManpowerTotalPositionsRequested.Should().Be(5);
    }

    [Fact]
    public void BuildSummary_ComputesCountsAndAverageApprovalDaysAcrossMultipleRequisitions()
    {
        var approved = NewRequisition(RequisitionStatus.Approved);
        AddHistory(approved, RequisitionStatus.Draft, RequisitionStatus.Submitted, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddHistory(approved, RequisitionStatus.UnderReview, RequisitionStatus.Approved, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        var rejected = NewRequisition(RequisitionStatus.Rejected);
        AddHistory(rejected, RequisitionStatus.Draft, RequisitionStatus.Submitted, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddHistory(rejected, RequisitionStatus.UnderReview, RequisitionStatus.Rejected, new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc));

        var stillPending = NewRequisition(RequisitionStatus.UnderReview);
        AddHistory(stillPending, RequisitionStatus.Draft, RequisitionStatus.Submitted, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var summary = ReportingCalculations.BuildSummary([approved, rejected, stillPending]);

        summary.TotalRequisitions.Should().Be(3);
        summary.ApprovedCount.Should().Be(1);
        summary.RejectedCount.Should().Be(1);
        summary.PendingCount.Should().Be(1);
        // Average over the two decided requisitions only (2 days, 5 days) - the still-pending one
        // contributes no processing-time data point, never a fabricated number.
        summary.AverageApprovalDays.Should().Be(3.5);
    }

    [Fact]
    public void BuildSummary_NoRequisitionsHaveReachedADecision_AverageApprovalDaysIsNull()
    {
        var summary = ReportingCalculations.BuildSummary([NewRequisition(RequisitionStatus.Draft)]);

        summary.AverageApprovalDays.Should().BeNull();
    }

    [Fact]
    public void BuildSummary_TopDepartments_RanksByCountDescending()
    {
        var itCategory = new RequisitionCategory { Name = "IT" };
        var it1 = NewRequisition(RequisitionStatus.Approved, itCategory);
        var it2 = NewRequisition(RequisitionStatus.Approved, itCategory);
        var hr = new Requisition { Status = RequisitionStatus.Approved, Category = itCategory, RequestedByUser = new User { FullName = "HR Person", Department = "HR" } };

        var summary = ReportingCalculations.BuildSummary([it1, it2, hr]);

        summary.TopDepartments.Should().ContainInOrder(
            new DepartmentCountDto("IT", 2),
            new DepartmentCountDto("HR", 1));
    }

    [Fact]
    public void BuildSummary_CategoryBreakdown_CountsEveryCategoryPresent()
    {
        var it = NewRequisition(RequisitionStatus.Approved, new RequisitionCategory { Name = "IT Equipment" });
        var manpower1 = NewRequisition(RequisitionStatus.Approved, new RequisitionCategory { Name = "Manpower" });
        var manpower2 = NewRequisition(RequisitionStatus.Approved, new RequisitionCategory { Name = "Manpower" });

        var summary = ReportingCalculations.BuildSummary([it, manpower1, manpower2]);

        summary.CategoryBreakdown.Should().ContainInOrder(
            new CategoryCountDto("Manpower", 2),
            new CategoryCountDto("IT Equipment", 1));
    }

    [Fact]
    public void BuildPersonReport_PartiallyApprovedIsCountedSeparately_NotFoldedIntoApproved()
    {
        // Unlike BuildSummary's ApprovedFamilyStatuses, My Report/All Users Report show Partially
        // Approved as its own distinct category the user explicitly asked for.
        var approved = NewRequisition(RequisitionStatus.Approved);
        var partiallyApproved = NewRequisition(RequisitionStatus.PartiallyApproved);
        var rejected = NewRequisition(RequisitionStatus.Rejected);

        var report = ReportingCalculations.BuildPersonReport(
            Guid.NewGuid(), "Emma Employee", "IT", [approved, partiallyApproved, rejected], ReportPeriod.Monthly);

        report.TotalCount.Should().Be(3);
        report.ApprovedCount.Should().Be(1);
        report.PartiallyApprovedCount.Should().Be(1);
        report.RejectedCount.Should().Be(1);
    }

    [Fact]
    public void BuildTrend_Monthly_GroupsBySubmissionMonth_WithPerOutcomeCounts()
    {
        var approvedInJan = NewRequisition(RequisitionStatus.Approved);
        approvedInJan.SubmittedAtUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var rejectedInJan = NewRequisition(RequisitionStatus.Rejected);
        rejectedInJan.SubmittedAtUtc = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
        var approvedInFeb = NewRequisition(RequisitionStatus.Approved);
        approvedInFeb.SubmittedAtUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        var trend = ReportingCalculations.BuildTrend([approvedInJan, rejectedInJan, approvedInFeb], ReportPeriod.Monthly);

        trend.Should().HaveCount(2);
        trend[0].Label.Should().Be("Jan 2026");
        trend[0].SubmittedCount.Should().Be(2);
        trend[0].ApprovedCount.Should().Be(1);
        trend[0].RejectedCount.Should().Be(1);
        trend[1].Label.Should().Be("Feb 2026");
        trend[1].SubmittedCount.Should().Be(1);
    }

    [Fact]
    public void BuildTrend_Weekly_GroupsBySubmissionWeekStart()
    {
        // 2026-01-05 and 2026-01-07 both fall in the Monday-Jan-5-2026 week; 2026-01-12 starts the next.
        var sameWeek1 = NewRequisition(RequisitionStatus.Approved);
        sameWeek1.SubmittedAtUtc = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var sameWeek2 = NewRequisition(RequisitionStatus.Approved);
        sameWeek2.SubmittedAtUtc = new DateTime(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc);
        var nextWeek = NewRequisition(RequisitionStatus.Approved);
        nextWeek.SubmittedAtUtc = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc);

        var trend = ReportingCalculations.BuildTrend([sameWeek1, sameWeek2, nextWeek], ReportPeriod.Weekly);

        trend.Should().HaveCount(2);
        trend[0].SubmittedCount.Should().Be(2);
        trend[1].SubmittedCount.Should().Be(1);
    }

    [Fact]
    public void BuildTrend_Yearly_GroupsBySubmissionYear()
    {
        var in2025 = NewRequisition(RequisitionStatus.Approved);
        in2025.SubmittedAtUtc = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var in2026a = NewRequisition(RequisitionStatus.Approved);
        in2026a.SubmittedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var in2026b = NewRequisition(RequisitionStatus.Rejected);
        in2026b.SubmittedAtUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);

        var trend = ReportingCalculations.BuildTrend([in2025, in2026a, in2026b], ReportPeriod.Yearly);

        trend.Should().HaveCount(2);
        trend[0].Label.Should().Be("2025");
        trend[0].SubmittedCount.Should().Be(1);
        trend[1].Label.Should().Be("2026");
        trend[1].SubmittedCount.Should().Be(2);
    }

    [Fact]
    public void BuildTrend_NeverSubmitted_ExcludedFromEveryBucket()
    {
        var draft = NewRequisition(RequisitionStatus.Draft); // SubmittedAtUtc left null

        var trend = ReportingCalculations.BuildTrend([draft], ReportPeriod.Monthly);

        trend.Should().BeEmpty();
    }
}
