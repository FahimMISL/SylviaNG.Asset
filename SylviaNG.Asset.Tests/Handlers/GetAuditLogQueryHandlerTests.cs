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
/// filtered set, not just the current page), and still enforces the pre-existing SystemAdmin-only
/// rule.</summary>
public class GetAuditLogQueryHandlerTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _companyId = Guid.NewGuid();

    private GetAuditLogQueryHandler BuildHandler() => new(_auditLogRepository.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_NonSystemAdmin_Forbidden()
    {
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(false);

        var act = () => BuildHandler().Handle(new GetAuditLogQuery(null, null, null, null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
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
                _companyId, null, null, null, null, null, null, null, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>(pageOfEntries, 47, 2, 10));

        var result = await BuildHandler().Handle(new GetAuditLogQuery(null, null, null, null, null, null, null, Page: 2, PageSize: 10), CancellationToken.None);

        result.TotalCount.Should().Be(47);
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_PageAndPageSizeOutOfRange_ClampToDefaults()
    {
        _currentUser.Setup(c => c.IsInRole(UserRole.SystemAdmin)).Returns(true);
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _auditLogRepository
            .Setup(r => r.GetFilteredAsync(
                _companyId, null, null, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([], 0, 1, 20));

        await BuildHandler().Handle(new GetAuditLogQuery(null, null, null, null, null, null, null, Page: -5, PageSize: 0), CancellationToken.None);

        _auditLogRepository.Verify(r => r.GetFilteredAsync(
            _companyId, null, null, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
