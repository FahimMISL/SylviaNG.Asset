using MediatR;
using RMS.Application.Features.ApprovalWorkflows.DTOs;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.CreateApprovalWorkflowVersion;

/// <summary>Clones the current published version into a new draft for editing.</summary>
public record CreateApprovalWorkflowVersionCommand(Guid WorkflowId) : IRequest<ApprovalWorkflowDto>;
