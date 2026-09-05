using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Common;
using RMS.Application.Features.AuditLogs.Queries.GetAuditLog;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>Feature 11: GetAuditLogQuery previously returned the entire filtered set and the frontend
/// paginated it client-side - confirms the handler now passes through real page/pageSize to the
/// repository and preserves TotalCount (so the UI's total-records count still reflects the whole
/// filtered set, not just the current page). Feature 8.1: SystemAdmin keeps the unrestricted,
/// cross-requisition console; anyone else may only open ONE requisition's own trail, and only if
/// RequisitionAccessHelper already says they can see that requisition at all.</summary>
public class GetAuditLogQueryHandlerTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IRequisitionRepository> _requisitionRepository = new();
    private readonly Mock<IApprovalDelegationRepository> _delegationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _companyId = Guid.NewGuid();

    private GetAuditLogQueryHandler BuildHandler() =>
        new(_auditLogRepository.Object, _requisitionRepository.Object, _delegationRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_NonSystemAdmin_NoRequisitionIdGiven_Forbidden()
    {
        // The unrestricted, cross-requisition query is SystemAdmin-only - a non-admin can never
        // omit RequisitionId to fish across every requisition's history.
        var userId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(false);

        var act = () => BuildHandler().Handle(new GetAuditLogQuery(null, null, null, null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_NonSystemAdmin_RequisitionTheyCannotAccess_Forbidden()
    {
        // Even with a RequisitionId, a non-admin who isn't the owner/approver/etc. on that specific
        // requisition (RequisitionAccessHelper) must still be blocked - scoping alone isn't enough.
        var userId = Guid.NewGuid();
        var requisitionId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(false);
        _requisitionRepository.Setup(r => r.GetByIdAsync(requisitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Requisition { Id = requisitionId, RequestedByUserId = Guid.NewGuid() });

        var act = () => BuildHandler().Handle(
            new GetAuditLogQuery(requisitionId, null, null, null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_NonSystemAdmin_OwnRequisition_AllowedAndScopedToJustThatOne()
    {
        // The Feature 8.1 fix itself: the requestor viewing their OWN requisition's "View Full Audit
        // Trail" must succeed, and the query must still be scoped by RequisitionId (never widened).
        var userId = Guid.NewGuid();
        var requisitionId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(false);
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _requisitionRepository.Setup(r => r.GetByIdAsync(requisitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Requisition { Id = requisitionId, RequestedByUserId = userId });
        _auditLogRepository
            .Setup(r => r.GetFilteredAsync(_companyId, requisitionId, null, null, null, null, null, null, 1, 20, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([], 0, 1, 20));

        await BuildHandler().Handle(new GetAuditLogQuery(requisitionId, null, null, null, null, null, null), CancellationToken.None);

        _auditLogRepository.Verify(
            r => r.GetFilteredAsync(_companyId, requisitionId, null, null, null, null, null, null, 1, 20, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TotalCountReflectsFullFilteredSet_WhileItemsRespectPageSize()
    {
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(true);
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);

        var pageOfEntries = new List<AuditLog>
        {
            new() { CompanyId = _companyId, ActionType = "UserRoleChanged", EntityName = nameof(RMS.Domain.Entities.User), ActorName = "Admin" },
        };
        _auditLogRepository
            .Setup(r => r.GetFilteredAsync(
                _companyId, null, null, null, null, null, null, null, 2, 10, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>(pageOfEntries, 47, 2, 10));

        var result = await BuildHandler().Handle(new GetAuditLogQuery(null, null, null, null, null, null, null, Page: 2, PageSize: 10), CancellationToken.None);

        result.TotalCount.Should().Be(47);
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_SortDescendingFalse_PassedThroughToRepository()
    {
        // Feature 8.2: clicking the Timestamp column a second time asks for ascending order - confirms
        // the handler forwards that through to the repository rather than always defaulting to true.
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(true);
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _auditLogRepository
            .Setup(r => r.GetFilteredAsync(
                _companyId, null, null, null, null, null, null, null, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([], 0, 1, 20));

        await BuildHandler().Handle(new GetAuditLogQuery(null, null, null, null, null, null, null, SortDescending: false), CancellationToken.None);

        _auditLogRepository.Verify(r => r.GetFilteredAsync(
            _companyId, null, null, null, null, null, null, null, 1, 20, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PageAndPageSizeOutOfRange_ClampToDefaults()
    {
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(true);
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _auditLogRepository
            .Setup(r => r.GetFilteredAsync(
                _companyId, null, null, null, null, null, null, null, 1, 20, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([], 0, 1, 20));

        await BuildHandler().Handle(new GetAuditLogQuery(null, null, null, null, null, null, null, Page: -5, PageSize: 0), CancellationToken.None);

        _auditLogRepository.Verify(r => r.GetFilteredAsync(
            _companyId, null, null, null, null, null, null, null, 1, 20, true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
