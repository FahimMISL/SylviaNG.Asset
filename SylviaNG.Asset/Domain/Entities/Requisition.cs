using RMS.Domain.Common;
using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// A requisition request per US-004/US-005/US-006/US-007. Status transitions
/// only ever go through the methods below, which consult
/// RequisitionStatusRules - nothing sets Status directly.
/// </summary>
public class Requisition : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid CategoryId { get; set; }
    public RequisitionCategory? Category { get; set; }

    public Guid RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }

    /// <summary>FR-RR-012 (deferred to a later pass): when set, distinct from RequestedByUserId - the
    /// manager/admin who actually performed the submission on the requestor's behalf.</summary>
    public Guid? SubmittedByUserId { get; set; }

    /// <summary>FR-RR-004: assigned on first submit, format REQ-{year}-{5-digit sequence}. Null while Draft.</summary>
    public string? RequisitionNumber { get; set; }

    public RequisitionPriority Priority { get; set; } = RequisitionPriority.Medium;
    public DateTime NeedByDate { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? Justification { get; set; }

    /// <summary>FR-RR-007/BR-RR-004: required, minimum 50 characters, only when Priority is High or Urgent.</summary>
    public string? UrgencyJustification { get; set; }

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

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
    public List<RequisitionStatusHistory> StatusHistory { get; set; } = new();
    public List<RequisitionAttachment> Attachments { get; set; } = new();

    /// <summary>Draft -> Submitted. Assigns the requisition number the first time only (a SentBack
    /// resubmission goes through Resubmit instead and keeps its original number).
    /// Returns the new history entry - callers on an already-tracked (pre-existing) Requisition
    /// must register it explicitly via IRequisitionRepository.AddStatusHistory rather than relying
    /// on the StatusHistory collection alone; EF Core can misdetect a new child added to an
    /// already-loaded collection on a tracked parent as an update instead of an insert (the same
    /// issue RequisitionRepository.ReplaceItems/ReplaceFieldValues already work around).</summary>
    public RequisitionStatusHistory Submit(string requisitionNumber, Guid actorUserId, string actorName, string? actorRole)
    {
        if (!RequisitionStatusRules.CanTransition(Status, RequisitionStatus.Submitted))
        {
            throw new InvalidOperationException($"Cannot submit a requisition in {Status} status.");
        }

        var from = Status;
        RequisitionNumber ??= requisitionNumber;
        Status = RequisitionStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
        var entry = new RequisitionStatusHistory
        {
            RequisitionId = Id,
            FromStatus = from,
            ToStatus = Status,
            ActorUserId = actorUserId,
            ActorName = actorName,
            ActorRole = actorRole,
        };
        StatusHistory.Add(entry);
        return entry;
    }

    /// <summary>FR-RR-008/BR-RR-002/BR-RR-003: only legal while Submitted and no approver has acted yet -
    /// enforced by RequisitionStatusRules, since every later status removes the Cancelled transition.
    /// See Submit's remarks re: registering the returned entry explicitly.</summary>
    public RequisitionStatusHistory Cancel(Guid actorUserId, string actorName, string? actorRole, string? comment)
    {
        if (!RequisitionStatusRules.CanTransition(Status, RequisitionStatus.Cancelled))
        {
            throw new InvalidOperationException(
                $"A requisition can only be cancelled while Submitted and before any approver action (current status: {Status}).");
        }

        var from = Status;
        Status = RequisitionStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        var entry = new RequisitionStatusHistory
        {
            RequisitionId = Id,
            FromStatus = from,
            ToStatus = Status,
            ActorUserId = actorUserId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = comment,
        };
        StatusHistory.Add(entry);
        return entry;
    }

    /// <summary>FR-RR-010: SentBack -> Submitted after the requestor has amended the requisition. Keeps the
    /// original RequisitionNumber and every prior StatusHistory entry - re-entering the workflow at the
    /// stage that sent it back is Feature 3's concern once it exists to route on this.
    /// See Submit's remarks re: registering the returned entry explicitly.</summary>
    public RequisitionStatusHistory Resubmit(Guid actorUserId, string actorName, string? actorRole, string? responseComment)
    {
        if (!RequisitionStatusRules.CanTransition(Status, RequisitionStatus.Submitted))
        {
            throw new InvalidOperationException($"Cannot resubmit a requisition in {Status} status.");
        }

        var from = Status;
        Status = RequisitionStatus.Submitted;
        var entry = new RequisitionStatusHistory
        {
            RequisitionId = Id,
            FromStatus = from,
            ToStatus = Status,
            ActorUserId = actorUserId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = responseComment,
        };
        StatusHistory.Add(entry);
        return entry;
    }
}
