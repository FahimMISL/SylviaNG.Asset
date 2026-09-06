using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;

namespace SylviaNG.Assets.Application.Extensions
{
    /// <summary>
    /// Feature 10 (US-031). Mirrors ValidationBehavior's shape exactly - a request only participates
    /// by implementing IPermissionGuardedRequest, so every other request in the app (the ~15 handlers
    /// with their own already-correct inline IsInRole checks) passes through completely untouched.
    /// </summary>
    public class PermissionAuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUser;

        public PermissionAuthorizationBehavior(IPermissionService permissionService, ICurrentUserService currentUser)
        {
            _permissionService = permissionService;
            _currentUser = currentUser;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is IPermissionGuardedRequest guarded)
            {
                var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
                var role = _currentUser.Role ?? throw new ForbiddenException();

                var allowed = await _permissionService.HasPermissionAsync(
                    companyId, role, guarded.Module, guarded.Action, cancellationToken);
                if (!allowed)
                {
                    throw new ForbiddenException();
                }
            }

            return await next();
        }
    }
}
