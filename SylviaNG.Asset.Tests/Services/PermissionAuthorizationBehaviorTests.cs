using FluentAssertions;
using MediatR;
using Moq;
using SylviaNG.Assets.Application.Common.Exceptions;
using SylviaNG.Assets.Application.Extensions;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace SylviaNG.Assets.Tests.Services;

public record GuardedTestRequest : IRequest<string>, IPermissionGuardedRequest
{
    public PermissionModule Module => PermissionModule.Rbac;
    public PermissionAction Action => PermissionAction.View;
}

public record UnguardedTestRequest : IRequest<string>;

/// <summary>Feature 10 (US-031): confirms the behavior is purely additive - a request that doesn't
/// implement IPermissionGuardedRequest passes through untouched, never even consulting
/// IPermissionService, exactly the property the ~15 pre-existing handlers with their own inline
/// IsInRole checks depend on.</summary>
public class PermissionAuthorizationBehaviorTests
{
    private readonly Mock<IPermissionService> _permissionService = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _companyId = Guid.NewGuid();

    public PermissionAuthorizationBehaviorTests()
    {
        _currentUser.Setup(c => c.CompanyId).Returns(_companyId);
        _currentUser.Setup(c => c.Role).Returns(UserRole.Employee);
    }

    private PermissionAuthorizationBehavior<TRequest, string> BuildBehavior<TRequest>() where TRequest : IRequest<string> =>
        new(_permissionService.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_GuardedRequest_Allowed_CallsNext()
    {
        _permissionService
            .Setup(p => p.HasPermissionAsync(_companyId, UserRole.Employee, PermissionModule.Rbac, PermissionAction.View, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var behavior = BuildBehavior<GuardedTestRequest>();
        var result = await behavior.Handle(new GuardedTestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_GuardedRequest_Denied_ThrowsForbidden_NeverCallsNext()
    {
        _permissionService
            .Setup(p => p.HasPermissionAsync(_companyId, UserRole.Employee, PermissionModule.Rbac, PermissionAction.View, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var nextCalled = false;
        var behavior = BuildBehavior<GuardedTestRequest>();

        var act = () => behavior.Handle(new GuardedTestRequest(), _ => { nextCalled = true; return Task.FromResult("ok"); }, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UnguardedRequest_PassesThrough_NeverConsultsPermissionService()
    {
        var behavior = BuildBehavior<UnguardedTestRequest>();

        var result = await behavior.Handle(new UnguardedTestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        _permissionService.Verify(
            p => p.HasPermissionAsync(It.IsAny<Guid>(), It.IsAny<UserRole>(), It.IsAny<PermissionModule>(), It.IsAny<PermissionAction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
