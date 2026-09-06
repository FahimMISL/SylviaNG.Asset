using MediatR;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.CreateApprovalWorkflow;

/// <summary>Creates the workflow shell + draft version 1 with nested stages/approvers/conditions/SLA
/// in one call, mirroring how CreateRequisitionCategory accepts nested field definitions.</summary>
public record CreateApprovalWorkflowCommand(
    string Name,
    string? Description,
    ApprovalWorkflowRoutingMode RoutingMode,
    bool AppliesToAllCategories,
    List<Guid> CategoryIds,
    List<ApprovalWorkflowStageInput> Stages,
    string? Notes) : IRequest<ApprovalWorkflowDto>;
