using MediatR;
using RMS.Application.Features.ApprovalWorkflows.DTOs;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.SetApprovalWorkflowActiveState;

/// <summary>Backs both ActivateApprovalWorkflowCommand and DeactivateApprovalWorkflowCommand from the
/// plan - same shape as RequisitionCategory's SetCategoryActiveStateCommand, one command + a bool.</summary>
public record SetApprovalWorkflowActiveStateCommand(Guid WorkflowId, bool IsActive) : IRequest<ApprovalWorkflowDto>;
