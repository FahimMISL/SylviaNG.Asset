using MediatR;

namespace RMS.Application.Features.Approvals.Commands.DelegateApprovalAction;

/// <summary>Ad-hoc single-task delegate - a one-time, permanent handoff of one specific pending
/// approval. Distinct from ApprovalDelegation (out-of-office). See the plan's "Delegation: two
/// distinct mechanisms" section.</summary>
public record DelegateApprovalActionCommand(Guid ApprovalId, Guid DelegateToUserId, string Comment) : IRequest;
