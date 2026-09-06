using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.Approvals.Queries.GetDelegations;

public class GetDelegationsQueryHandler : IRequestHandler<GetDelegationsQuery, List<ApprovalDelegationDto>>
{
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetDelegationsQueryHandler(IApprovalDelegationRepository delegationRepository, ICurrentUserService currentUser)
    {
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
    }

    public async Task<List<ApprovalDelegationDto>> Handle(GetDelegationsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var delegations = await _delegationRepository.GetAllAsync(companyId, cancellationToken);
        return delegations.Select(ApprovalDelegationDto.FromEntity).ToList();
    }
}
