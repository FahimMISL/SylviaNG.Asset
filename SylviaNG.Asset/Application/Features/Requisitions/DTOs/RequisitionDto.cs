using RMS.Application.Features.Approvals.DTOs;
using RMS.Application.Features.Procurement.DTOs;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.DTOs;

public record RequisitionItemDto(Guid Id, string ItemName, int Quantity, Guid? CategoryItemId)
{
    public static RequisitionItemDto FromEntity(RequisitionItem i) => new(i.Id, i.ItemName, i.Quantity, i.CategoryItemId);
}

public record RequisitionFieldValueDto(Guid FieldDefinitionId, string Label, string? Value)
{
    public static RequisitionFieldValueDto FromEntity(RequisitionFieldValue v) => new(
        v.FieldDefinitionId, v.FieldDefinition?.Label ?? string.Empty, v.Value);
}

/// <summary>FR-RR-009: one timeline entry.</summary>
public record RequisitionStatusHistoryDto(
    string? FromStatus, string ToStatus, Guid ActorUserId, string ActorName, string? ActorRole, string? Comment, DateTime TimestampUtc)
{
    public static RequisitionStatusHistoryDto FromEntity(RequisitionStatusHistory h) => new(
        h.FromStatus?.ToString(), h.ToStatus.ToString(), h.ActorUserId, h.ActorName, h.ActorRole, h.Comment, h.CreatedAtUtc);
}

/// <summary>US-007: attachment metadata - no blob/content here, just what the requisition detail list needs.</summary>
public record RequisitionAttachmentDto(
    Guid Id, string FileName, string ContentType, long SizeBytes, string UploadedByName, DateTime UploadedAtUtc)
{
    public static RequisitionAttachmentDto FromEntity(RequisitionAttachment a) => new(
        a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedByName, a.CreatedAtUtc);
}

public record RequisitionDto(
    Guid Id,
    string? RequisitionNumber,
    /// <summary>Lets the frontend determine whether the current viewer IS the requestor, without
    /// inferring it - needed to gate requestor-only actions like Respond to Clarification.</summary>
    Guid RequestedByUserId,
    Guid CategoryId,
    string CategoryName,
    int CategoryVersionNumber,
    string Priority,
    DateTime? NeedByDate,
    decimal EstimatedCost,
    string? Justification,
    string? UrgencyJustification,
    Guid? CostCenterId,
    string? CostCenterName,
    string? ProjectCode,
    string Status,
    DateTime? SubmittedAtUtc,
    DateTime? CancelledAtUtc,
    DateTime CreatedAtUtc,
    List<RequisitionItemDto> Items,
    List<RequisitionFieldValueDto> FieldValues,
    List<RequisitionStatusHistoryDto> Timeline,
    List<RequisitionAttachmentDto> Attachments,
    /// <summary>Feature 3: resolved workflow version, stages with status/approvers/SLA state, and the
    /// action history - null while the requisition hasn't been submitted into a workflow yet. Read-only:
    /// approvers still cannot edit requisition fields through this DTO. Its CurrentUserCanAct flag is
    /// the one authoritative, delegation-aware "should I show approve/reject/etc. buttons" signal -
    /// see ApprovalProcessDto's remarks.</summary>
    ApprovalProcessDto? ApprovalProcess,
    /// <summary>Feature 5: null until Status reaches Approved or later. Read-only for the requestor
    /// (satisfies rule 10's "requester visibility of procurement/fulfillment progress" for free, since
    /// the requestor already passes GetRequisitionByIdQueryHandler's owner check) - its
    /// CurrentUserCanProcess flag is the one authoritative "show Start Processing/Record
    /// Fulfillment/Close buttons" signal, same role as ApprovalProcessDto.CurrentUserCanAct.</summary>
    ProcurementDto? Procurement)
{
    private static readonly HashSet<RequisitionStatus> ProcurementPipelineStatuses =
    [
        RequisitionStatus.Approved, RequisitionStatus.PartiallyApproved, RequisitionStatus.InProcurement,
        RequisitionStatus.PartiallyFulfilled, RequisitionStatus.Fulfilled, RequisitionStatus.Closed,
    ];

    /// <summary>currentUserCanAct/currentUserCanProcess default false for call sites that don't need
    /// them (e.g. the requestor's own view of a requisition they just created/listed) -
    /// GetRequisitionByIdQueryHandler is the one place that computes them properly.</summary>
    public static RequisitionDto FromEntity(Requisition r, bool currentUserCanAct = false, bool currentUserCanProcess = false) => new(
        r.Id,
        r.RequisitionNumber,
        r.RequestedByUserId,
        r.CategoryId,
        r.Category?.Name ?? string.Empty,
        r.CategoryVersionNumber,
        r.Priority.ToString(),
        r.NeedByDate,
        r.EstimatedCost,
        r.Justification,
        r.UrgencyJustification,
        r.CostCenterId,
        r.CostCenter?.Name,
        r.ProjectCode,
        r.Status.ToString(),
        r.SubmittedAtUtc,
        r.CancelledAtUtc,
        r.CreatedAtUtc,
        r.Items.Select(RequisitionItemDto.FromEntity).ToList(),
        r.FieldValues.Select(RequisitionFieldValueDto.FromEntity).ToList(),
        r.StatusHistory.Select(RequisitionStatusHistoryDto.FromEntity).ToList(),
        r.Attachments.Select(RequisitionAttachmentDto.FromEntity).ToList(),
        r.ApprovalProcess is null ? null : ApprovalProcessDto.FromEntity(r.ApprovalProcess, DateTime.UtcNow, currentUserCanAct),
        ProcurementPipelineStatuses.Contains(r.Status) ? ProcurementDto.FromEntity(r, currentUserCanProcess) : null);
}

public record RequisitionSummaryDto(
    Guid Id,
    string? RequisitionNumber,
    string CategoryName,
    string Status,
    string Priority,
    DateTime? NeedByDate,
    decimal EstimatedCost,
    DateTime CreatedAtUtc,
    int ItemCount,
    string? RequesterName)
{
    public static RequisitionSummaryDto FromEntity(Requisition r) => new(
        r.Id, r.RequisitionNumber, r.Category?.Name ?? string.Empty, r.Status.ToString(), r.Priority.ToString(),
        r.NeedByDate, r.EstimatedCost, r.CreatedAtUtc, r.Items.Count, r.RequestedByUser?.FullName);
}
