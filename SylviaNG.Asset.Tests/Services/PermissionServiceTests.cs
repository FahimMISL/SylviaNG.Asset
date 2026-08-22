using FluentAssertions;
using Moq;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;
using RMS.Infrastructure.Services;

namespace SylviaNG.Assets.Tests.Services;

/// <summary>Feature 10 (US-031).</summary>
public class PermissionServiceTests
{
    private readonly Mock<IRolePermissionRepository> _rolePermissionRepository = new();
    private readonly Guid _companyId = Guid.NewGuid();

    private PermissionService BuildService() => new(_rolePermissionRepository.Object);

    [Fact]
    public async Task HasPermissionAsync_SystemAdmin_AlwaysAllowed_NeverConsultsTheTable()
    {
        var service = BuildService();

        var result = await service.HasPermissionAsync(
            _companyId, UserRole.SystemAdmin, PermissionModule.Rbac, PermissionAction.Delete, CancellationToken.None);

        result.Should().BeTrue();
        _rolePermissionRepository.Verify(
            r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<PermissionModule>(), It.IsAny<PermissionAction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HasPermissionAsync_GrantedRow_ReturnsTrue()
    {
        _rolePermissionRepository
            .Setup(r => r.GetAsync(_companyId, UserRole.Employee, PermissionModule.EligibilityPolicy, PermissionAction.View, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RolePermission { CompanyId = _companyId, Role = UserRole.Employee, Module = PermissionModule.EligibilityPolicy, Action = PermissionAction.View, IsAllowed = true });

        var service = BuildService();

        var result = await service.HasPermissionAsync(
            _companyId, UserRole.Employee, PermissionModule.EligibilityPolicy, PermissionAction.View, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_NoRow_FailsClosed()
    {
        _rolePermissionRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<PermissionModule>(), It.IsAny<PermissionAction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolePermission?)null);

        var service = BuildService();

        var result = await service.HasPermissionAsync(
            _companyId, UserRole.Employee, PermissionModule.EligibilityPolicy, PermissionAction.View, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_ExplicitlyDeniedRow_ReturnsFalse()
    {
        _rolePermissionRepository
            .Setup(r => r.GetAsync(_companyId, UserRole.Employee, PermissionModule.Rbac, PermissionAction.Edit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RolePermission { CompanyId = _companyId, Role = UserRole.Employee, Module = PermissionModule.Rbac, Action = PermissionAction.Edit, IsAllowed = false });

        var service = BuildService();

        var result = await service.HasPermissionAsync(
            _companyId, UserRole.Employee, PermissionModule.Rbac, PermissionAction.Edit, CancellationToken.None);

        result.Should().BeFalse();
    }
}
