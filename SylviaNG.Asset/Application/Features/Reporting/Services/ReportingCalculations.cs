using RMS.Application.Features.Reporting.DTOs;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Reporting.Services;

/// <summary>
/// Feature 7 (US-022/US-025): pure, DB-free calculations over an already-loaded set of Requisitions -
/// mirrors ProcurementService.GetApprovedCeilings's "pure static logic, no repository dependency"
/// shape from Feature 5, so this is unit-testable without mocks or a database. Every number here is
/// derived from data Features 1-6 already produce; nothing is fabricated, and there is deliberately
/// no budget/vendor/inventory/finance-rule calculation anywhere - that data doesn't exist in this
/// system at all.
/// </summary>
public static class ReportingCalculations
{
    private static readonly HashSet<RequisitionStatus> ProcurementRelevantStatuses =
    [
        RequisitionStatus.Approved, RequisitionStatus.PartiallyApproved, RequisitionStatus.InProcurement,
        RequisitionStatus.PartiallyFulfilled, RequisitionStatus.Fulfilled, RequisitionStatus.Closed,
    ];

    private static readonly HashSet<RequisitionStatus> ApprovalDecisionStatuses =
    [
        RequisitionStatus.Approved, RequisitionStatus.Rejected, RequisitionStatus.PartiallyApproved,
    ];

    private static readonly HashSet<RequisitionStatus> PendingStatuses =
    [
        RequisitionStatus.Submitted, RequisitionStatus.UnderReview, RequisitionStatus.SentBack,
    ];

    private static readonly HashSet<RequisitionStatus> ApprovedFamilyStatuses =
    [
        RequisitionStatus.Approved, RequisitionStatus.PartiallyApproved, RequisitionStatus.InProcurement,
        RequisitionStatus.PartiallyFulfilled, RequisitionStatus.Fulfilled, RequisitionStatus.Closed,
    ];

    private static readonly HashSet<RequisitionStatus> FulfilledFamilyStatuses =
    [
        RequisitionStatus.Fulfilled, RequisitionStatus.PartiallyFulfilled, RequisitionStatus.Closed,
    ];

    private static readonly HashSet<RequisitionStatus> ProcurementActiveStatuses =
    [
        RequisitionStatus.InProcurement, RequisitionStatus.PartiallyFulfilled,
    ];

    public const string EligibilityBlockNote =
        "Eligibility policy blocks are not logged as a historical event in the current system - a submission that fails an eligibility check never gets persisted, so a blocked-request count can't be reliably reported here.";

    public static string BuildItemsSummary(Requisition r) =>
        string.Join(", ", r.Items.Select(i => $"{i.ItemName} x{i.Quantity}"));

    /// <summary>Human-readable approval status. Reuses Requisition.Status as the source of truth
    /// (not a new stored field); UnderReview is broken down further to the specific pending stage
    /// name when the approval process data is loaded.</summary>
    public static string DescribeApprovalStatus(Requisition r) => r.Status switch
    {
        RequisitionStatus.Draft => "Draft",
        RequisitionStatus.Submitted => "Awaiting Review",
        RequisitionStatus.UnderReview => DescribePendingStage(r),
        RequisitionStatus.Rejected => "Rejected",
        RequisitionStatus.PartiallyApproved => "Partially Approved",
        RequisitionStatus.SentBack => "Sent Back for Correction",
        RequisitionStatus.Cancelled => "Cancelled",
        _ => "Fully Approved", // Approved, InProcurement, Fulfilled, PartiallyFulfilled, Closed
    };

    private static string DescribePendingStage(Requisition r)
    {
        var pendingStage = r.ApprovalProcess?.StageInstances.FirstOrDefault(s =>
            s.Status is RequisitionApprovalStatus.Pending or RequisitionApprovalStatus.InProgress or RequisitionApprovalStatus.ClarificationRequested);
        return pendingStage?.ApprovalWorkflowStage is not null ? $"Pending: {pendingStage.ApprovalWorkflowStage.Name}" : "Under Review";
    }

    /// <summary>Null unless the requisition has actually reached the procurement pipeline (Approved
    /// or later) - never shown for anything still in the approval chain.</summary>
    public static string? DescribeProcurementStatus(Requisition r)
    {
        if (!ProcurementRelevantStatuses.Contains(r.Status))
        {
            return null;
        }

        return r.Status switch
        {
            RequisitionStatus.Approved or RequisitionStatus.PartiallyApproved => "Awaiting Processing",
            RequisitionStatus.InProcurement => "In Procurement",
            RequisitionStatus.PartiallyFulfilled => "Partially Fulfilled",
            RequisitionStatus.Fulfilled => "Fulfilled",
            RequisitionStatus.Closed => "Closed",
            _ => null,
        };
    }

    /// <summary>Submitted -> approval-decision elapsed days. Null while still in progress (never
    /// submitted, or submitted but no Approved/Rejected/PartiallyApproved decision yet) - a
    /// still-open requisition never gets a fabricated processing time.</summary>
    public static double? ComputeProcessingDays(Requisition r)
    {
        var submittedAt = r.StatusHistory
            .Where(h => h.ToStatus == RequisitionStatus.Submitted)
            .OrderBy(h => h.CreatedAtUtc)
            .Select(h => (DateTime?)h.CreatedAtUtc)
            .FirstOrDefault();
        if (submittedAt is null)
        {
            return null;
        }

        var decidedAt = r.StatusHistory
            .Where(h => ApprovalDecisionStatuses.Contains(h.ToStatus))
            .OrderByDescending(h => h.CreatedAtUtc)
            .Select(h => (DateTime?)h.CreatedAtUtc)
            .FirstOrDefault();
        if (decidedAt is null)
        {
            return null;
        }

        return (decidedAt.Value - submittedAt.Value).TotalDays;
    }

    public static OperationalReportRowDto ToReportRow(Requisition r) => new(
        r.Id,
        r.RequisitionNumber,
        r.RequestedByUser?.FullName ?? string.Empty,
        r.RequestedByUser?.Department,
        r.Category?.Name ?? string.Empty,
        BuildItemsSummary(r),
        r.Items.Sum(i => i.Quantity),
        r.Status.ToString(),
        r.Priority.ToString(),
        r.SubmittedAtUtc ?? r.CreatedAtUtc,
        DescribeApprovalStatus(r),
        DescribeProcurementStatus(r),
        r.NeedByDate,
        ComputeProcessingDays(r));

    /// <summary>US-025. requisitions should be the full, unfiltered company set - this is a snapshot,
    /// not a filtered report.</summary>
    public static ExecutiveSummaryDto BuildSummary(List<Requisition> requisitions)
    {
        var total = requisitions.Count;
        var pending = requisitions.Count(r => PendingStatuses.Contains(r.Status));
        var approved = requisitions.Count(r => ApprovedFamilyStatuses.Contains(r.Status));
        var rejected = requisitions.Count(r => r.Status == RequisitionStatus.Rejected);
        var fulfilled = requisitions.Count(r => FulfilledFamilyStatuses.Contains(r.Status));
        var procurementActive = requisitions.Count(r => ProcurementActiveStatuses.Contains(r.Status));

        var pendingApprovals = requisitions
            .Where(r => r.ApprovalProcess is not null)
            .SelectMany(r => r.ApprovalProcess!.StageInstances)
            .Count(s => s.Status is RequisitionApprovalStatus.Pending or RequisitionApprovalStatus.InProgress or RequisitionApprovalStatus.ClarificationRequested);

        // Feature 6: a manpower requisition's own quantity total already sums every line
        // (Software Engineer x3 + HR Executive x2 = 5) - counting requisitions and positions
        // separately keeps "how many requests" and "how many positions" from ever being conflated.
        var manpowerRequisitions = requisitions
            .Where(r => string.Equals(r.Category?.Name?.Trim(), "Manpower", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var manpowerCount = manpowerRequisitions.Count;
        var manpowerPositions = manpowerRequisitions.SelectMany(r => r.Items).Sum(i => i.Quantity);

        var processingDaysValues = requisitions.Select(ComputeProcessingDays).Where(d => d.HasValue).Select(d => d!.Value).ToList();
        double? averageApprovalDays = processingDaysValues.Count > 0 ? processingDaysValues.Average() : null;

        var trend = requisitions
            .Where(r => r.SubmittedAtUtc.HasValue)
            .GroupBy(r => new DateTime(r.SubmittedAtUtc!.Value.Year, r.SubmittedAtUtc.Value.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new MonthlyTrendPointDto(g.Key.ToString("MMM yyyy"), g.Count()))
            .ToList();

        return new ExecutiveSummaryDto(
            total, pending, approved, rejected, fulfilled, pendingApprovals, procurementActive,
            manpowerCount, manpowerPositions, averageApprovalDays, trend, EligibilityBlockNote);
    }
}
