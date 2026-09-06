using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Dashboard.Queries.GetManpowerSummary;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>Feature 12 (HR Manager dashboard). Manpower/View alone isn't a precise enough RBAC gate
/// for this company-wide read - DepartmentHead also holds that grant (for their own department) but
/// must not get this. Also confirms the "MR-001 = 5, not 1" aggregation rule from the task spec.</summary>
public class GetManpowerSummaryQueryHandlerTests
{
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _companyId = Guid.NewGuid();

    private GetManpowerSummaryQueryHandler BuildHandler() => new(_requisitionRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_DepartmentHead_Forbidden_EvenThoughTheyHoldManpowerView()
    {
        _currentUser.Setup(c => c.IsInRole(UserRole.HrManager)).Returns(false);
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(false);

        var act = () => BuildHandler().Handle(new GetManpowerSummaryQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        _requisitionRepository.Verify(r => r.GetManpowerForCompanyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HrManager_AggregatesQuantitiesAcrossLines_NotCountedAsItemCount()
    {
        _currentUser.Setup(c => c.IsInRole(UserRole.HrManager)).Returns(true);
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);

        // MR-001 from the task spec: Software Engineer x3 + HR Executive x2 = 5, not 2 (item count).
        var mr001 = new Requisition
        {
            CompanyId = _companyId,
            RequisitionNumber = "MR-001",
            Status = RequisitionStatus.Approved,
            RequestedByUser = new User { FullName = "Diana Head" },
        };
        mr001.Items.Add(new RequisitionItem { ItemName = "Software Engineer", Quantity = 3 });
        mr001.Items.Add(new RequisitionItem { ItemName = "HR Executive", Quantity = 2 });

        _requisitionRepository.Setup(r => r.GetManpowerForCompanyAsync(_companyId, It.IsAny<CancellationToken>())).ReturnsAsync([mr001]);

        var result = await BuildHandler().Handle(new GetManpowerSummaryQuery(), CancellationToken.None);

        result.TotalPositionsRequested.Should().Be(5);
        result.RecentRequisitions.Should().ContainSingle(r => r.RequisitionNumber == "MR-001" && r.TotalQuantity == 5);
        result.TopPositions.Should().ContainSingle(p => p.PositionName == "Software Engineer" && p.Quantity == 3);
    }

    [Fact]
    public async Task Handle_SystemAdmin_Allowed()
    {
        _currentUser.Setup(c => c.IsInRole(UserRole.HrManager)).Returns(false);
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(true);
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _requisitionRepository.Setup(r => r.GetManpowerForCompanyAsync(_companyId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await BuildHandler().Handle(new GetManpowerSummaryQuery(), CancellationToken.None);

        result.TotalPositionsRequested.Should().Be(0);
    }
}
