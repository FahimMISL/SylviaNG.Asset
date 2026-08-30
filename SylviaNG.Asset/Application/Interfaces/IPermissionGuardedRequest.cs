using RMS.Domain.Enums;

namespace RMS.Application.Interfaces;

/// <summary>
/// Feature 10 (US-031): a MediatR request opts into permission-matrix enforcement by implementing
/// this. Requests that don't implement it are untouched by PermissionAuthorizationBehavior - this is
/// purely additive, not a replacement for the existing inline IsInRole checks elsewhere.
/// </summary>
public interface IPermissionGuardedRequest
{
    PermissionModule Module { get; }
    PermissionAction Action { get; }
}
