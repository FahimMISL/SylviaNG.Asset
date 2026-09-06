using RMS.Domain.Enums;

namespace RMS.Domain.Entities;

/// <summary>
/// Feature 4: 0..1 child of an EligibilityPolicy - its mere presence means a replacement/waiting-
/// period restriction is enabled (e.g. "laptop every 24 months"); toggling the restriction off on the
/// admin form just means this row is removed on save (see EligibilityPolicyRepository.
/// SetReplacementRule), not a separate IsEnabled flag.
/// </summary>
public class EligibilityPolicyReplacementRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EligibilityPolicyId { get; set; }
    public EligibilityPolicy? EligibilityPolicy { get; set; }

    public int DurationValue { get; set; }
    public EligibilityDurationUnit DurationUnit { get; set; }
}
