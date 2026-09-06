using MediatR;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Commands.UpdateRequisition;

/// <summary>US-004: edit an existing Draft ("Continue Draft"), and/or submit it. Also backs FR-RR-010's
/// SentBack amendment flow - the same edit shape, gated on status in the handler.</summary>
public record UpdateRequisitionCommand(
    Guid Id,
    Guid CategoryId,
    List<RequisitionItemInput> Items,
    RequisitionPriority Priority,
    DateTime? NeedByDate,
    decimal EstimatedCost,
    string? Justification,
    string? UrgencyJustification,
    Guid? CostCenterId,
    string? ProjectCode,
    List<RequisitionFieldValueInput> FieldValues,
    string? ResubmitComment,
    bool Submit) : IRequest<RequisitionDto>;
