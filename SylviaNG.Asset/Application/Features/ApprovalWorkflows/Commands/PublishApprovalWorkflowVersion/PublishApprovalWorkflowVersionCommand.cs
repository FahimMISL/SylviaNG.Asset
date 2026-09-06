using MediatR;
using RMS.Application.Features.ApprovalWorkflows.DTOs;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.PublishApprovalWorkflowVersion;

/// <summary>Marks a draft version published (immutable from then on), and bumps
/// ApprovalWorkflow.CurrentVersionNumber to point new requisitions at it.</summary>
public record PublishApprovalWorkflowVersionCommand(Guid WorkflowId, Guid VersionId) : IRequest<ApprovalWorkflowDto>;
