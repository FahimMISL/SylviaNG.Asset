using MediatR;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.UpdateApprovalWorkflowVersion;

/// <summary>Wholesale-replaces a draft (unpublished) version's nested structure. Fails if the version
/// is already published - published versions are immutable, per the plan.</summary>
public record UpdateApprovalWorkflowVersionCommand(
    Guid WorkflowId,
    Guid VersionId,
    ApprovalWorkflowRoutingMode RoutingMode,
    bool AppliesToAllCategories,
    List<Guid> CategoryIds,
    List<ApprovalWorkflowStageInput> Stages,
    string? Notes) : IRequest<ApprovalWorkflowDto>;
