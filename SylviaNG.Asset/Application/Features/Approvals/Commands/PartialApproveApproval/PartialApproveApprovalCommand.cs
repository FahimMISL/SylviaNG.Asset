using MediatR;
using RMS.Application.Features.Approvals.DTOs;

namespace RMS.Application.Features.Approvals.Commands.PartialApproveApproval;

/// <summary>Always terminal - Requisition.PartialApprove() (UnderReview -> PartiallyApproved), never
/// leaves the process pending further stages, per the plan.</summary>
public record PartialApproveApprovalCommand(Guid ApprovalId, string Comment, List<PartialApprovalDecisionInput> Decisions) : IRequest;
