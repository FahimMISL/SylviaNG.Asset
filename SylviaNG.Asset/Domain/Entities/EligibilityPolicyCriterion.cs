using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Feature 4: one allowed value for one attribute type on an EligibilityPolicy. Semantics (enforced
/// by PolicyEvaluationService, not by this entity itself): multiple rows of the SAME CriterionType
/// are OR'd together (e.g. two Department rows = "IT" OR "Finance" both qualify); rows of DIFFERENT
/// CriterionType are AND'd (e.g. a Grade row AND a Department row means both axes must match). A
/// policy with zero criteria of a given type is open on that axis (no restriction). Deliberately a
/// flat value-match against free-text tags, not a ranking/hierarchy system - there is no master-data
/// table for Grade/Designation/etc. to rank against.
/// </summary>
public class EligibilityPolicyCriterion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EligibilityPolicyId { get; set; }
    public EligibilityPolicy? EligibilityPolicy { get; set; }

    public EligibilityCriterionType CriterionType { get; set; }
    public string AllowedValue { get; set; } = string.Empty;
}
