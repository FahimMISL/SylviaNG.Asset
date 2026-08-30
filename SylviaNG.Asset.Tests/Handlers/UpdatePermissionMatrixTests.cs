using FluentAssertions;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Rbac.Commands.UpdatePermissionMatrix;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Handlers;

/// <summary>Feature 10 (US-031). The matrix UI's Save always round-trips the full Module x Action grid
/// (66 cells), not just the ones the admin actually touched - most of those cells are false with no
/// existing row. Confirms the handler doesn't materialize a row for an untouched false cell (a real bug
/// found on recheck: a completely no-op save was silently ballooning the RolePermissions table from ~10
/// rows to 66 per role, live-confirmed via psql before this test was written).</summary>
public class UpdatePermissionMatrixTests
{
    private readonly Mock<IRolePermissionRepository> _rolePermissionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _companyId = Guid.NewGuid();

    private UpdatePermissionMatrixCommandHandler BuildHandler() =>
        new(_rolePermissionRepository.Object, _currentUser.Object, _auditLogger.Object, _unitOfWork.Object);

    public UpdatePermissionMatrixTests()
    {
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
    }

    [Fact]
    public async Task Handle_UnchangedFalseCellWithNoExistingRow_DoesNotAddAnything()
    {
        _rolePermissionRepository
            .Setup(r => r.GetAsync(_companyId, UserRole.Employee, PermissionModule.EligibilityPolicy, PermissionAction.View, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolePermission?)null);

        var command = new UpdatePermissionMatrixCommand(
            UserRole.Employee, [new PermissionCellInput(PermissionModule.EligibilityPolicy, PermissionAction.View, false)]);

        await BuildHandler().Handle(command, CancellationToken.None);

        _rolePermissionRepository.Verify(r => r.Add(It.IsAny<RolePermission>()), Times.Never);
        _auditLogger.Verify(
            a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NewTrueCellWithNoExistingRow_AddsItAndAudits()
    {
        _rolePermissionRepository
            .Setup(r => r.GetAsync(_companyId, UserRole.Employee, PermissionModule.EligibilityPolicy, PermissionAction.View, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolePermission?)null);

        var command = new UpdatePermissionMatrixCommand(
            UserRole.Employee, [new PermissionCellInput(PermissionModule.EligibilityPolicy, PermissionAction.View, true)]);

        await BuildHandler().Handle(command, CancellationToken.None);

        _rolePermissionRepository.Verify(r => r.Add(It.Is<RolePermission>(p => p.IsAllowed && p.Module == PermissionModule.EligibilityPolicy)), Times.Once);
        _auditLogger.Verify(
            a => a.LogAsync("PermissionMatrixUpdated", nameof(RolePermission), Guid.Empty, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SystemAdminRole_RejectedRegardlessOfPayload()
    {
        var command = new UpdatePermissionMatrixCommand(UserRole.SystemAdmin, []);

        var act = () => BuildHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _rolePermissionRepository.Verify(r => r.Add(It.IsAny<RolePermission>()), Times.Never);
    }
}
