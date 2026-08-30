using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions.Queries.GetDepartmentRequisitions;
using RMS.Application.Features.Requisitions.Queries.GetRequisitionById;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>Feature 10 (US-032): DepartmentHead's own data-scoping - no prior art existed for this
/// before Feature 10, see RequisitionAccessHelper's remarks.</summary>
public class RequisitionDepartmentAccessTests
{
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<IApprovalDelegationRepository> _delegationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private readonly Guid _requisitionId = Guid.NewGuid();
    private readonly Guid _requestorId = Guid.NewGuid();
    private readonly Guid _deptHeadId = Guid.NewGuid();

    public RequisitionDepartmentAccessTests()
    {
        // Mirrors CurrentUserService's real IsInRole(role) => Role == role - Moq doesn't derive this
        // automatically from the Role setup, so every test needs it wired once, here.
        _currentUser.Setup(c => c.IsInRole(It.IsAny<UserRole>())).Returns((UserRole role) => _currentUser.Object.Role == role);
    }

    private Requisition BuildRequisition(string? requestorDepartment) => new()
    {
        Id = _requisitionId,
        RequestedByUserId = _requestorId,
        RequestedByUser = new User { Id = _requestorId, Department = requestorDepartment },
        Status = RequisitionStatus.UnderReview,
    };

    [Fact]
    public async Task GetById_DepartmentHead_SameDepartmentAsRequestor_Allowed()
    {
        _currentUser.Setup(c => c.UserId).Returns(_deptHeadId);
        _currentUser.Setup(c => c.Role).Returns(UserRole.DepartmentHead);
        _currentUser.Setup(c => c.Department).Returns("IT");
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequisition("IT"));

        var handler = new GetRequisitionByIdQueryHandler(_requisitionRepository.Object, _delegationRepository.Object, _currentUser.Object);
        var result = await handler.Handle(new GetRequisitionByIdQuery(_requisitionId), CancellationToken.None);

        result.Id.Should().Be(_requisitionId);
    }

    [Fact]
    public async Task GetById_DepartmentHead_DifferentDepartmentFromRequestor_StillForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(_deptHeadId);
        _currentUser.Setup(c => c.Role).Returns(UserRole.DepartmentHead);
        _currentUser.Setup(c => c.Department).Returns("IT");
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequisition("Procurement"));

        var handler = new GetRequisitionByIdQueryHandler(_requisitionRepository.Object, _delegationRepository.Object, _currentUser.Object);
        var act = () => handler.Handle(new GetRequisitionByIdQuery(_requisitionId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetById_DepartmentHead_NullDepartmentOnEitherSide_NeverMatches()
    {
        _currentUser.Setup(c => c.UserId).Returns(_deptHeadId);
        _currentUser.Setup(c => c.Role).Returns(UserRole.DepartmentHead);
        _currentUser.Setup(c => c.Department).Returns((string?)null);
        _requisitionRepository.Setup(r => r.GetByIdAsync(_requisitionId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequisition(null));

        var handler = new GetRequisitionByIdQueryHandler(_requisitionRepository.Object, _delegationRepository.Object, _currentUser.Object);
        var act = () => handler.Handle(new GetRequisitionByIdQuery(_requisitionId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetDepartmentRequisitions_NonDepartmentHead_Forbidden()
    {
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);

        var handler = new GetDepartmentRequisitionsQueryHandler(_requisitionRepository.Object, _currentUser.Object);
        var act = () => handler.Handle(new GetDepartmentRequisitionsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetDepartmentRequisitions_DepartmentHead_ScopesByOwnDepartment()
    {
        var companyId = Guid.NewGuid();
        _currentUser.Setup(c => c.Role).Returns(UserRole.DepartmentHead);
        _currentUser.Setup(c => c.CompanyId).Returns(companyId);
        _currentUser.Setup(c => c.Department).Returns("IT");
        _requisitionRepository
            .Setup(r => r.GetForDepartmentAsync(companyId, "IT", It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildRequisition("IT")]);

        var handler = new GetDepartmentRequisitionsQueryHandler(_requisitionRepository.Object, _currentUser.Object);
        var result = await handler.Handle(new GetDepartmentRequisitionsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        _requisitionRepository.Verify(r => r.GetForDepartmentAsync(companyId, "IT", It.IsAny<CancellationToken>()), Times.Once);
    }
}
