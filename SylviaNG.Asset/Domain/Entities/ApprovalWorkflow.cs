using RMS.Domain.Common;

namespace RMS.Domain.Entities;

/// <summary>
/// Feature 3: a configurable, per-company multi-stage approval workflow (aggregate root).
/// Mirrors RequisitionCategory's shape - unique (CompanyId, Name), Activate/Deactivate toggle,
/// version history owned as a child collection.
/// </summary>
public class ApprovalWorkflow : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int CurrentVersionNumber { get; set; }

    public List<ApprovalWorkflowVersion> Versions { get; set; } = new();

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
