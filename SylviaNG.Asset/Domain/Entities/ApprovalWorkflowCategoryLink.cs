namespace RMS.Domain.Entities;

/// <summary>Join entity, mirrors CategoryCostCenterLink. Only consulted when the owning version's
/// AppliesToAllCategories=false.</summary>
public class ApprovalWorkflowCategoryLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApprovalWorkflowVersionId { get; set; }
    public ApprovalWorkflowVersion? ApprovalWorkflowVersion { get; set; }

    public Guid RequisitionCategoryId { get; set; }
    public RequisitionCategory? RequisitionCategory { get; set; }
}
