using MediatR;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.DeleteApprovalWorkflowDraftVersion;

public record DeleteApprovalWorkflowDraftVersionCommand(Guid WorkflowId, Guid VersionId) : IRequest;
