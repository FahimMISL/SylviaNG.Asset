using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Users.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserSummaryDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetUsersQueryHandler(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<List<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var users = await _userRepository.GetAllAsync(companyId, request.Role, cancellationToken);
        return users.Select(UserSummaryDto.FromEntity).ToList();
    }
}
