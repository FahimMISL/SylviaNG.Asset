using RMS.Domain.Common;
using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Immutable once Published - mirrors CategoryTemplateVersion's draft/publish idiom. While
/// IsPublished=false, the whole nested structure (stages/approvers/conditions/SLA) is freely
/// replaceable via one update command. Once published, only a new version (cloned from it) can
/// change anything - requisitions that already resolved this version keep it forever
/// (RequisitionApprovalProcess.ApprovalWorkflowVersionId), same snapshot precedent as
/// Requisition.CategoryVersionNumber.
/// </summary>
public class ApprovalWorkflowVersion : AuditableEntity
{
    public Guid ApprovalWorkflowId { get; set; }
    public ApprovalWorkflow? ApprovalWorkflow { get; set; }

    public int VersionNumber { get; set; }

    /// <summary>UI label/default only - see ApprovalWorkflowRoutingMode.</summary>
    public ApprovalWorkflowRoutingMode RoutingMode { get; set; }

    public bool AppliesToAllCategories { get; set; } = true;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public string? Notes { get; set; }

    public List<ApprovalWorkflowStage> Stages { get; set; } = new();
    public List<ApprovalWorkflowCategoryLink> CategoryLinks { get; set; } = new();
}
