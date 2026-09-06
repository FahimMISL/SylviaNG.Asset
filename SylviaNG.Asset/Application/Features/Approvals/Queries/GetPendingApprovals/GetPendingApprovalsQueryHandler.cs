using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Approvals.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Approvals.Queries.GetPendingApprovals;

public class GetPendingApprovalsQueryHandler : IRequestHandler<GetPendingApprovalsQuery, List<PendingApprovalDto>>
{
    private readonly IRequisitionApprovalRepository _requisitionApprovalRepository;
    private readonly IApprovalDelegationRepository _delegationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetPendingApprovalsQueryHandler(
        IRequisitionApprovalRepository requisitionApprovalRepository, IApprovalDelegationRepository delegationRepository, ICurrentUserService currentUser)
    {
        _requisitionApprovalRepository = requisitionApprovalRepository;
        _delegationRepository = delegationRepository;
        _currentUser = currentUser;
    }

    public async Task<List<PendingApprovalDto>> Handle(GetPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Delegation-aware: I see my own assignments AND anyone's who is currently delegating to me
        // (out-of-office ApprovalDelegation) - resolved dynamically, never stored on the assignment row.
        var delegators = await _delegationRepository.GetDelegatorsForAsync(userId, today, cancellationToken);
        var candidateUserIds = new List<Guid> { userId };
        candidateUserIds.AddRange(delegators);

        var assignments = await _requisitionApprovalRepository.GetPendingAssignmentsAsync(candidateUserIds, cancellationToken);

        var now = DateTime.UtcNow;
        return assignments
            .Select(a => PendingApprovalDto.FromEntity(a, isViaDelegation: a.AssignedUserId != userId, now))
            .OrderByDescending(p => Enum.Parse<RequisitionPriority>(p.Priority))
            .ThenBy(p => p.SubmittedAtUtc)
            .ToList();
    }
}
