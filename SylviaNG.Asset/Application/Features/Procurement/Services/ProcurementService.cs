using FluentValidation;
using FluentValidation.Results;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Procurement.Services;

/// <summary>
/// Feature 5's core engine - the one place procurement/fulfillment business rules live, mirroring
/// ApprovalWorkflowEngine's role for Feature 3. Called from thin command handlers.
/// </summary>
public class ProcurementService
{
    private readonly IRequisitionRepository _requisitionRepository;

    public ProcurementService(IRequisitionRepository requisitionRepository)
    {
        _requisitionRepository = requisitionRepository;
    }

    /// <summary>Per-item ceiling for how much may be procured/fulfilled: the PartialApprovalDecision's
    /// ApprovedQuantity if this requisition was PartiallyApproved (already loaded on
    /// requisition.ApprovalProcess by IRequisitionRepository.GetByIdAsync/GetForProcurementAsync),
    /// otherwise the item's full requested Quantity.</summary>
    public static Dictionary<Guid, int> GetApprovedCeilings(Requisition requisition)
    {
        var partialDecisions = requisition.ApprovalProcess?.StageInstances
            .SelectMany(s => s.Actions)
            .Where(a => a.ActionType == ApprovalActionType.PartialApprove)
            .SelectMany(a => a.PartialDecisions)
            .ToList() ?? [];

        return requisition.Items.ToDictionary(
            item => item.Id,
            item => partialDecisions.FirstOrDefault(d => d.RequisitionItemId == item.Id)?.ApprovedQuantity ?? item.Quantity);
    }

    public void StartProcessing(Requisition requisition, Guid actorUserId, string actorName, string? actorRole, string? comment)
    {
        if (requisition.Status is not (RequisitionStatus.Approved or RequisitionStatus.PartiallyApproved))
        {
            throw new ConflictException($"Only Approved or Partially Approved requisitions can enter procurement (current status: {requisition.Status}).");
        }

        var missingPriceItems = requisition.Items.Where(i => i.CategoryItem?.Price is null).Select(i => i.ItemName).ToList();
        if (missingPriceItems.Count > 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("Items", $"The following item(s) have no price set - ask an admin to set a price in Category Setup before starting procurement: {string.Join(", ", missingPriceItems)}."),
            });
        }

        var ceilings = GetApprovedCeilings(requisition);
        var totalAmount = requisition.Items.Sum(i => (i.CategoryItem!.Price!.Value) * ceilings[i.Id]);

        var entry = requisition.StartProcurement(actorUserId, actorName, actorRole, comment);
        _requisitionRepository.AddStatusHistory(entry);

        _requisitionRepository.AddProcurementRecord(new RequisitionProcurementRecord
        {
            RequisitionId = requisition.Id,
            ActionType = ProcurementActionType.StartProcessing,
            ActorUserId = actorUserId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = comment,
            TotalProcurementAmount = totalAmount,
            LineItems = requisition.Items.Select(i => new RequisitionProcurementLineItem
            {
                RequisitionItemId = i.Id,
                UnitPrice = i.CategoryItem!.Price!.Value,
            }).ToList(),
        });
    }

    public void RecordFulfillment(
        Requisition requisition, Guid actorUserId, string actorName, string? actorRole, string? comment, List<(Guid ItemId, int Quantity)> fulfillments)
    {
        if (requisition.Status is not (RequisitionStatus.InProcurement or RequisitionStatus.PartiallyFulfilled))
        {
            throw new ConflictException($"Fulfillment can only be recorded while a requisition is In Procurement or Partially Fulfilled (current status: {requisition.Status}).");
        }

        var toApply = fulfillments.Where(f => f.Quantity > 0).ToList();
        if (toApply.Count == 0)
        {
            throw new ValidationException(new[] { new ValidationFailure("Items", "Record at least one fulfilled quantity.") });
        }

        var ceilings = GetApprovedCeilings(requisition);
        var itemsById = requisition.Items.ToDictionary(i => i.Id);
        var failures = new List<ValidationFailure>();

        foreach (var (itemId, quantity) in toApply)
        {
            if (!itemsById.TryGetValue(itemId, out var item))
            {
                failures.Add(new ValidationFailure("Items", "One of the selected items does not belong to this requisition."));
                continue;
            }

            var remaining = ceilings[itemId] - item.FulfilledQuantity;
            if (quantity > remaining)
            {
                failures.Add(new ValidationFailure("Items", $"Cannot fulfill more than the approved remaining quantity for '{item.ItemName}' ({remaining} remaining)."));
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        foreach (var (itemId, quantity) in toApply)
        {
            itemsById[itemId].FulfilledQuantity += quantity;
        }

        var isFullyFulfilled = requisition.Items.All(i => i.FulfilledQuantity >= ceilings[i.Id]);
        if (isFullyFulfilled)
        {
            var entry = requisition.Fulfill(actorUserId, actorName, actorRole, comment);
            _requisitionRepository.AddStatusHistory(entry);
        }
        else if (requisition.Status == RequisitionStatus.InProcurement)
        {
            var entry = requisition.PartiallyFulfill(actorUserId, actorName, actorRole, comment);
            _requisitionRepository.AddStatusHistory(entry);
        }
        // else: already PartiallyFulfilled and still incomplete - status unchanged, just record the ledger entry.

        _requisitionRepository.AddProcurementRecord(new RequisitionProcurementRecord
        {
            RequisitionId = requisition.Id,
            ActionType = ProcurementActionType.RecordFulfillment,
            ActorUserId = actorUserId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = comment,
            LineItems = toApply.Select(f => new RequisitionProcurementLineItem
            {
                RequisitionItemId = f.ItemId,
                QuantityFulfilledThisAction = f.Quantity,
            }).ToList(),
        });
    }

    public void Close(Requisition requisition, Guid actorUserId, string actorName, string? actorRole, string? comment)
    {
        if (requisition.Status is not (RequisitionStatus.Fulfilled or RequisitionStatus.PartiallyFulfilled))
        {
            throw new ConflictException($"Only a Fulfilled or Partially Fulfilled requisition can be closed (current status: {requisition.Status}).");
        }

        var entry = requisition.Close(actorUserId, actorName, actorRole, comment);
        _requisitionRepository.AddStatusHistory(entry);

        _requisitionRepository.AddProcurementRecord(new RequisitionProcurementRecord
        {
            RequisitionId = requisition.Id,
            ActionType = ProcurementActionType.Close,
            ActorUserId = actorUserId,
            ActorName = actorName,
            ActorRole = actorRole,
            Comment = comment,
        });
    }
}
