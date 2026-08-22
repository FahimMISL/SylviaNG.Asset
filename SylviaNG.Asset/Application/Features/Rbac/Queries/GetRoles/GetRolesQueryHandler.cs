using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Rbac.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Rbac.Queries.GetRoles;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleSummaryDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetRolesQueryHandler(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<List<RoleSummaryDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var roles = new List<RoleSummaryDto>();
        foreach (var role in Enum.GetValues<UserRole>())
        {
            var users = await _userRepository.GetAllAsync(companyId, role, cancellationToken);
            roles.Add(new RoleSummaryDto(role, RoleLabel(role), users.Count(u => u.IsActive), users.Count));
        }

        return roles;
    }

    private static string RoleLabel(UserRole role) => role switch
    {
        UserRole.Employee => "Employee",
        UserRole.LineManager => "Line Manager",
        UserRole.DepartmentHead => "Department Head",
        UserRole.ProcurementOfficer => "Procurement Officer",
        UserRole.HrManager => "HR Manager",
        UserRole.SystemAdmin => "System Admin",
        UserRole.Ceo => "CEO",
        _ => role.ToString(),
    };
}
