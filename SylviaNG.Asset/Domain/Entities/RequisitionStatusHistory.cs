using RMS.Domain.Common;
using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>FR-RR-009: one entry per status transition, backing the requisition detail timeline.</summary>
public class RequisitionStatusHistory : AuditableEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition? Requisition { get; set; }

    public RequisitionStatus? FromStatus { get; set; }
    public RequisitionStatus ToStatus { get; set; }

    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? ActorRole { get; set; }
    public string? Comment { get; set; }
}
