using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.Approvals.Queries.GetMyDelegations;

public class GetMyDelegationsQueryHandler : IRequestHandler<GetMyDelegationsQuery, List<ApprovalDelegationDto>>
{
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyDelegationsQueryHandler(IApprovalDelegationRepository delegationRepository, ICurrentUserService currentUser)
    {
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
    }

    public async Task<List<ApprovalDelegationDto>> Handle(GetMyDelegationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var delegations = await _delegationRepository.GetForDelegatorAsync(userId, cancellationToken);
        return delegations.Select(ApprovalDelegationDto.FromEntity).ToList();
    }
}
