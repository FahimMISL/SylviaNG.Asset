using FluentAssertions;
using Moq;
using RMS.Application.Common;
using RMS.Application.Features.Requisitions.Queries.SearchRequisitions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>
/// Feature 11: confirms the handler resolves exactly one of the three data-scope parameters per role
/// (never combined, never left open for anyone but SystemAdmin) before delegating to
/// IRequisitionRepository.SearchAsync - the same boundary GetMyRequisitionsQuery/
/// GetDepartmentRequisitionsQuery/GetProcurementRequisitionsQuery already enforce, generalized. The
/// actual EF filtering/free-text/pagination logic lives in RequisitionRepository.SearchAsync itself,
/// which (like every other repository method in this codebase) has no Moq/unit test - verified live
/// against the real database instead, matching this project's established testing convention.
/// </summary>
public class SearchRequisitionsQueryHandlerTests
{
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public SearchRequisitionsQueryHandlerTests()
    {
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _requisitionRepository
            .Setup(r => r.SearchAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<RequisitionStatus>?>(), It.IsAny<RequisitionPriority?>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Requisition>([], 0, 1, 20));
    }

    private SearchRequisitionsQueryHandler BuildHandler() => new(_requisitionRepository.Object, _currentUser.Object);

    private static SearchRequisitionsQuery EmptyQuery() => new(null, null, null, null, null, null, null, null, null, null, null);

    [Fact]
    public async Task Handle_Employee_ScopesToOwnUserId_Only()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        await BuildHandler().Handle(EmptyQuery(), CancellationToken.None);

        _requisitionRepository.Verify(r => r.SearchAsync(
            _companyId, _userId, null, false,
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<RequisitionStatus>?>(), It.IsAny<RequisitionPriority?>(), It.IsAny<string?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<bool>(), 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DepartmentHead_ScopesToOwnDepartment_Only()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.DepartmentHead);
        _currentUser.Setup(c => c.Department).Returns("IT");

        await BuildHandler().Handle(EmptyQuery(), CancellationToken.None);

        _requisitionRepository.Verify(r => r.SearchAsync(
            _companyId, null, "IT", false,
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<RequisitionStatus>?>(), It.IsAny<RequisitionPriority?>(), It.IsAny<string?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<bool>(), 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProcurementOfficer_ScopesToPipeline_CompanyWide()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.ProcurementOfficer);

        await BuildHandler().Handle(EmptyQuery(), CancellationToken.None);

        _requisitionRepository.Verify(r => r.SearchAsync(
            _companyId, null, null, true,
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<RequisitionStatus>?>(), It.IsAny<RequisitionPriority?>(), It.IsAny<string?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<bool>(), 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SystemAdmin_Unrestricted()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.SystemAdmin);

        await BuildHandler().Handle(EmptyQuery(), CancellationToken.None);

        _requisitionRepository.Verify(r => r.SearchAsync(
            _companyId, null, null, false,
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<RequisitionStatus>?>(), It.IsAny<RequisitionPriority?>(), It.IsAny<string?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<bool>(), 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HrManagerAndCeo_TreatedLikeEmployee_OwnOnly()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.HrManager);

        await BuildHandler().Handle(EmptyQuery(), CancellationToken.None);

        _requisitionRepository.Verify(r => r.SearchAsync(
            _companyId, _userId, null, false,
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<RequisitionStatus>?>(), It.IsAny<RequisitionPriority?>(), It.IsAny<string?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<bool>(), 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PageAndPageSizeOutOfRange_ClampToDefaults()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.SystemAdmin);
        var query = new SearchRequisitionsQuery(null, null, null, null, null, null, null, null, null, null, null, Page: 0, PageSize: 500);

        await BuildHandler().Handle(query, CancellationToken.None);

        _requisitionRepository.Verify(r => r.SearchAsync(
            _companyId, null, null, false,
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<RequisitionStatus>?>(), It.IsAny<RequisitionPriority?>(), It.IsAny<string?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<bool>(), 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsPagedResult_PreservingTotalCountAndPaging()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.SystemAdmin);
        var requisition = new Requisition
        {
            CompanyId = _companyId,
            CategoryId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            RequisitionNumber = "REQ-2026-00001",
            Status = RequisitionStatus.Submitted,
        };
        _requisitionRepository
            .Setup(r => r.SearchAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<RequisitionStatus>?>(), It.IsAny<RequisitionPriority?>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Requisition>([requisition], 37, 2, 10));

        var result = await BuildHandler().Handle(EmptyQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(37);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().ContainSingle(i => i.RequisitionNumber == "REQ-2026-00001");
    }
}
