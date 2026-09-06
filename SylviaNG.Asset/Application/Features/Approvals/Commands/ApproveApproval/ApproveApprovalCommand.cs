using MediatR;

namespace RMS.Application.Features.Approvals.Commands.ApproveApproval;

/// <summary>EstimatedCost is required (>0) only when the target stage is CapturesEstimatedCost - see
/// the plan's Context section on why the approver, not the employee, supplies it.</summary>
public record ApproveApprovalCommand(Guid ApprovalId, string Comment, decimal? EstimatedCost) : IRequest;
