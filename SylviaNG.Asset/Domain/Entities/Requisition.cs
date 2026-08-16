using RMS.Domain.Common;
using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// A requisition request per US-004. Minimal foundation: a fixed field set
/// (Item/Quantity/Priority/Need By Date/Justification), no per-category
/// dynamic field rendering, no approval workflow yet (Feature 3) and no real
/// attachment storage yet (Feature 16) - those are deliberately out of scope
/// for this slice.
/// </summary>
public class Requisition : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid CategoryId { get; set; }
    public RequisitionCategory? Category { get; set; }

    public Guid RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }

    public RequisitionPriority Priority { get; set; } = RequisitionPriority.Medium;
    public DateTime NeedByDate { get; set; }
    public string? Justification { get; set; }
    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
    public DateTime? SubmittedAtUtc { get; set; }

    /// <summary>US-001 AC6: the category's CurrentVersionNumber at the time this requisition was saved,
    /// so it's on record which template version was in effect when the employee submitted.</summary>
    public int CategoryVersionNumber { get; set; }

    /// <summary>US-003: cost center chosen from the category's linked list, mandatory when the category requires it.</summary>
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }

    /// <summary>US-003: mandatory/optional/not-applicable per the category's ProjectCodeRequirement.</summary>
    public string? ProjectCode { get; set; }

    public List<RequisitionItem> Items { get; set; } = new();
    public List<RequisitionFieldValue> FieldValues { get; set; } = new();

    public void Submit()
    {
        Status = RequisitionStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
    }
}
