namespace RMS.Domain.Enums;

/// <summary>Feature 4: the unit EligibilityPolicyReplacementRule.DurationValue is expressed in (e.g.
/// "laptop every 24 Months"). Deliberately separate from SlaDurationUnit (BusinessHours/CalendarDays) -
/// a replacement waiting period is a calendar concept, not a working-hours one.</summary>
public enum EligibilityDurationUnit
{
    Days = 0,
    Months = 1,
    Years = 2,
}
