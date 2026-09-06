namespace RMS.Domain.Enums;

/// <summary>Feature 4: which User attribute an EligibilityPolicyCriterion row matches against.
/// Multiple rows of the SAME type on one policy are OR'd; rows of DIFFERENT types are AND'd - see
/// PolicyEvaluationService.</summary>
public enum EligibilityCriterionType
{
    Grade = 0,
    Designation = 1,
    EmploymentType = 2,
    Department = 3,
    Location = 4,
}
