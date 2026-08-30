namespace RMS.Domain.Enums;

/// <summary>Feature 4: one of the flat eligibility attributes on User (nullable there, since existing
/// seeded users won't have it set until an admin/HR updates their record).</summary>
public enum EmploymentType
{
    Permanent = 0,
    Contractual = 1,
    Probation = 2,
    Intern = 3,
}
