using RMS.Application.Features.Procurement.Services;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Procurement.DTOs;

/// <summary>One row in the Procurement Queue/Processing/Completed list - richer than
/// RequisitionSummaryDto since a Procurement Officer needs requester/department context that
/// summary doesn't carry.</summary>
public record ProcurementQueueItemDto(
    Guid Id, string? RequisitionNumber, string CategoryName, string RequesterName, string? RequesterDepartment,
    string Priority, DateTime? NeedByDate, string Status, decimal? TotalProcurementAmount, DateTime CreatedAtUtc)
{
    public static ProcurementQueueItemDto FromEntity(Requisition r)
    {
        var startRecord = r.ProcurementRecords.FirstOrDefault(p => p.ActionType == ProcurementActionType.StartProcessing);
        var totalAmount = startRecord?.TotalProcurementAmount
            ?? (r.Items.All(i => i.CategoryItem?.Price is not null)
                ? r.Items.Sum(i => i.CategoryItem!.Price!.Value * ProcurementService.GetApprovedCeilings(r)[i.Id])
                : null);

        return new ProcurementQueueItemDto(
            r.Id, r.RequisitionNumber, r.Category?.Name ?? string.Empty, r.RequestedByUser?.FullName ?? string.Empty,
            r.RequestedByUser?.Department, r.Priority.ToString(), r.NeedByDate, r.Status.ToString(), totalAmount, r.CreatedAtUtc);
    }
}
