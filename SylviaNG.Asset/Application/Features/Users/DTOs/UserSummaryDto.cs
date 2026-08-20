using RMS.Domain.Entities;

namespace RMS.Application.Features.Users.DTOs;

/// <summary>Minimal user-directory row - the picker source for approvers/fallback approvers/
/// escalation contacts/delegates across the admin and approver UIs (workflow builder, delegation
/// form, etc), and for Feature 4's eligibility policy form to offer real Grade/Designation/
/// Department/Location values instead of free text an admin can mistype or aim at the wrong field.
/// Deliberately thin: no PasswordHash or CompanyId, just enough to render a picker.</summary>
public record UserSummaryDto(
    Guid Id, string FullName, string Email, string Role, bool IsActive,
    string? Grade, string? Designation, string? EmploymentType, string? Department, string? Location)
{
    public static UserSummaryDto FromEntity(User u) => new(
        u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive,
        u.Grade, u.Designation, u.EmploymentType?.ToString(), u.Department, u.Location);
}
