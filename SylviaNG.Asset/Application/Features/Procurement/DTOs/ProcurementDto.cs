using RMS.Application.Features.Procurement.Services;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Procurement.DTOs;

public record ProcurementLineItemDto(
    Guid RequisitionItemId, string ItemName, int RequestedQuantity, int ApprovedQuantity,
    int FulfilledQuantity, int RemainingQuantity, decimal? UnitPrice, decimal? LineTotal);

public record ProcurementRecordLineEntryDto(string ItemName, decimal? UnitPrice, int? QuantityFulfilledThisAction);

public record ProcurementRecordDto(
    string ActionType, Guid ActorUserId, string ActorName, string? ActorRole, string? Comment,
    decimal? TotalProcurementAmount, DateTime TimestampUtc, List<ProcurementRecordLineEntryDto> LineItems)
{
    /// <summary>itemNames avoids needing a .ThenInclude(l => l.RequisitionItem) on the ledger's line
    /// items - the parent Requisition's own Items collection already has every name we need.</summary>
    public static ProcurementRecordDto FromEntity(RequisitionProcurementRecord r, Dictionary<Guid, string> itemNames) => new(
        r.ActionType.ToString(), r.ActorUserId, r.ActorName, r.ActorRole, r.Comment, r.TotalProcurementAmount, r.CreatedAtUtc,
        r.LineItems.Select(l => new ProcurementRecordLineEntryDto(
            itemNames.GetValueOrDefault(l.RequisitionItemId, string.Empty), l.UnitPrice, l.QuantityFulfilledThisAction)).ToList());
}

/// <summary>Null on a RequisitionDto until Status reaches Approved or later. CurrentUserCanProcess is
/// the one authoritative "should the frontend show Start Processing/Record Fulfillment/Close buttons"
/// signal, computed by GetRequisitionByIdQueryHandler - mirrors ApprovalProcessDto's CurrentUserCanAct.</summary>
public record ProcurementDto(
    List<ProcurementLineItemDto> Items, decimal? TotalProcurementAmount, bool IsFullyFulfilled,
    List<ProcurementRecordDto> History, bool CurrentUserCanProcess)
{
    public static ProcurementDto FromEntity(Requisition requisition, bool currentUserCanProcess)
    {
        var ceilings = ProcurementService.GetApprovedCeilings(requisition);
        var startRecord = requisition.ProcurementRecords.FirstOrDefault(r => r.ActionType == ProcurementActionType.StartProcessing);
        var priceByItemId = startRecord?.LineItems.ToDictionary(l => l.RequisitionItemId, l => l.UnitPrice)
            ?? requisition.Items.ToDictionary(i => i.Id, i => i.CategoryItem?.Price);

        var items = requisition.Items.Select(i =>
        {
            var approvedQuantity = ceilings[i.Id];
            var unitPrice = priceByItemId.GetValueOrDefault(i.Id);
            return new ProcurementLineItemDto(
                i.Id, i.ItemName, i.Quantity, approvedQuantity, i.FulfilledQuantity,
                approvedQuantity - i.FulfilledQuantity, unitPrice, unitPrice.HasValue ? unitPrice.Value * approvedQuantity : null);
        }).ToList();

        var itemNames = requisition.Items.ToDictionary(i => i.Id, i => i.ItemName);

        return new ProcurementDto(
            items,
            startRecord?.TotalProcurementAmount,
            requisition.Items.Count > 0 && requisition.Items.All(i => i.FulfilledQuantity >= ceilings[i.Id]),
            requisition.ProcurementRecords.Select(r => ProcurementRecordDto.FromEntity(r, itemNames)).ToList(),
            currentUserCanProcess);
    }
}
