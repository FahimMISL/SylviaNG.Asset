using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Domain.Enums;

namespace RMS.Api.Controllers.Requests;

/// <summary>Shared body shape for both CreateApprovalWorkflow (WorkflowId/VersionId come from the
/// route on Update, not this body) and UpdateApprovalWorkflowVersion.</summary>
public record SaveApprovalWorkflowVersionRequestBody(
    string Name,
    string? Description,
    ApprovalWorkflowRoutingMode RoutingMode,
    bool AppliesToAllCategories,
    List<Guid> CategoryIds,
    List<ApprovalWorkflowStageInput> Stages,
    string? Notes);

public record UpdateApprovalWorkflowVersionRequestBody(
    ApprovalWorkflowRoutingMode RoutingMode,
    bool AppliesToAllCategories,
    List<Guid> CategoryIds,
    List<ApprovalWorkflowStageInput> Stages,
    string? Notes);
