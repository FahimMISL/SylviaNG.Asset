namespace RMS.Domain.Enums;

/// <summary>Per US-003 AC3: project code field can be set as optional or mandatory per category.</summary>
public enum ProjectCodeRequirement
{
    NotApplicable = 0,
    Optional = 1,
    Mandatory = 2,
}
