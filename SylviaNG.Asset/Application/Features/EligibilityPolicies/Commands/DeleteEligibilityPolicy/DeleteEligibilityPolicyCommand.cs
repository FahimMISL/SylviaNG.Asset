using MediatR;

namespace RMS.Application.Features.EligibilityPolicies.Commands.DeleteEligibilityPolicy;

/// <summary>Deletes freely - unlike an ApprovalWorkflow, a policy's past evaluations aren't tracked
/// anywhere (no audit-critical historical record references it), so there's nothing to check for
/// before removing it. Cascades to its Criteria and ReplacementRule.</summary>
public record DeleteEligibilityPolicyCommand(Guid PolicyId) : IRequest;
