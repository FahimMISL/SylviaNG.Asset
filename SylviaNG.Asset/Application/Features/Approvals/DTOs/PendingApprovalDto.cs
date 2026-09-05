using RMS.Application.Features.Approvals.Services;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Approvals.DTOs;

/// <summary>One line of the requested item, for the inbox's Item/Price columns - Price is the Admin's
/// catalog price on the CategoryItem this line resolved from (null if the Admin never set one), never
/// typed by the requestor or approver.</summary>
public record PendingApprovalItemDto(string ItemName, int Quantity, decimal? Price)
{
    public static PendingApprovalItemDto FromEntity(RequisitionItem i) => new(i.ItemName, i.Quantity, i.CategoryItem?.Price);
}

/// <summary>One row in the current user's approval inbox - one RequisitionApprovalAssignment they can
/// (or, if delegated away, could) act on.</summary>
public record PendingApprovalDto(
    Guid AssignmentId,
    Guid ApprovalId,
    Guid RequisitionId,
    string? RequisitionNumber,
    string CategoryName,
    List<PendingApprovalItemDto> Items,
    string Priority,
    decimal EstimatedCost,
    DateTime? NeedByDate,
    DateTime SubmittedAtUtc,
    int StageOrder,
    string StageName,
    bool CapturesEstimatedCost,
    bool IsRequired,
    DateTime? SlaDueUtc,
    string SlaState,
    /// <summary>True when this row is showing up because someone delegated their assignment to the
    /// current user (out-of-office ApprovalDelegation), not because they were the original assignee.</summary>
    bool IsViaDelegation)
{
    public static PendingApprovalDto FromEntity(RequisitionApprovalAssignment assignment, bool isViaDelegation, DateTime nowUtc)
    {
        var approval = assignment.RequisitionApproval!;
        var requisition = approval.RequisitionApprovalProcess!.Requisition!;
        var slaState = SlaStateCalculator.Compute(approval.SlaStartUtc, approval.SlaDueUtc, approval.SlaPausedAtUtc, nowUtc);

        return new(
            assignment.Id, approval.Id, requisition.Id, requisition.RequisitionNumber,
            requisition.Category?.Name ?? string.Empty, requisition.Items.Select(PendingApprovalItemDto.FromEntity).ToList(),
            requisition.Priority.ToString(), requisition.EstimatedCost,
            requisition.NeedByDate, requisition.SubmittedAtUtc ?? requisition.CreatedAtUtc,
            approval.StageOrder, approval.ApprovalWorkflowStage?.Name ?? string.Empty,
            approval.ApprovalWorkflowStage?.CapturesEstimatedCost ?? false,
            assignment.IsRequired, approval.SlaDueUtc, slaState.ToString(), isViaDelegation);
    }
}
