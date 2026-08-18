using MediatR;
using RMS.Application.Features.Approvals.DTOs;

namespace RMS.Application.Features.Approvals.Queries.GetPendingApprovals;

/// <summary>The current user's approval inbox, sorted Priority then SubmittedAt per the plan.</summary>
public record GetPendingApprovalsQuery : IRequest<List<PendingApprovalDto>>;
