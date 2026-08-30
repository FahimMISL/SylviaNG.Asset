namespace RMS.Application.Features.Approvals.DTOs;

/// <summary>DeclineReason is required by the command validator whenever DeclinedQuantity > 0 - not a
/// DB constraint, per the plan.</summary>
public record PartialApprovalDecisionInput(Guid RequisitionItemId, int ApprovedQuantity, int DeclinedQuantity, string? DeclineReason);
