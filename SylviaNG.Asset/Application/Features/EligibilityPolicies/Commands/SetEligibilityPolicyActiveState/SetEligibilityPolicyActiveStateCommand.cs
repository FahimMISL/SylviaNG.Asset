using MediatR;
using RMS.Application.Features.EligibilityPolicies.DTOs;

namespace RMS.Application.Features.EligibilityPolicies.Commands.SetEligibilityPolicyActiveState;

/// <summary>Backs both Activate and Deactivate - same shape as ApprovalWorkflow's
/// SetApprovalWorkflowActiveStateCommand / RequisitionCategory's SetCategoryActiveStateCommand.</summary>
public record SetEligibilityPolicyActiveStateCommand(Guid PolicyId, bool IsActive) : IRequest<EligibilityPolicyDto>;
