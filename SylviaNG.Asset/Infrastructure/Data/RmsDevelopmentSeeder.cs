using Microsoft.EntityFrameworkCore;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Infrastructure.Data;

/// <summary>
/// DEV-ONLY. Real login (Keycloak) isn't wired on the frontend yet, so there is no
/// authenticated user to attribute Feature 1/2 writes to. Seeds one Company and one
/// System Admin user so CurrentUserService has a real identity to fall back to when
/// there's no authenticated principal (see CurrentUserService.FallbackDevUser). Never
/// runs outside Development - see Program.cs. Idempotent: safe to run on every startup.
/// </summary>
public static class RmsDevelopmentSeeder
{
    public static async Task SeedAsync(RmsDbContext context)
    {
        var company = await context.Companies.FirstOrDefaultAsync();
        if (company is null)
        {
            company = new Company { Name = "Demo Company", CreatedAtUtc = DateTime.UtcNow };
            context.Companies.Add(company);
            await context.SaveChangesAsync();
        }

        var adminExists = await context.Users.AnyAsync(u => u.Role == UserRole.SystemAdmin);
        if (!adminExists)
        {
            context.Users.Add(new User
            {
                CompanyId = company.Id,
                FullName = "Demo User (local dev stub)",
                Email = "demo@rms.local",
                Role = UserRole.SystemAdmin,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        // Feature 3 (Approval Workflow) needs more than one identity to exercise
        // submit-as-X / approve-as-Y locally, since there's no real login yet -
        // see CurrentUserService's X-Dev-User-Id header support. Idempotent by email.
        // Feature 4: each also gets sensible sample eligibility attributes (Grade/Designation/
        // EmploymentType/Department/Location) so eligibility policies are actually testable locally -
        // Liam and Diana sit at a higher Grade than Emma so a "Grade: Manager and above" style policy
        // has something real to differentiate against.
        var demoActors = new[]
        {
            ("Emma Employee (dev stub)", "emma.employee@rms.local", UserRole.Employee,
                "Officer", "Software Engineer", EmploymentType.Permanent, "IT", "Head Office"),
            ("Liam Manager (dev stub)", "liam.manager@rms.local", UserRole.LineManager,
                "Manager", "IT Manager", EmploymentType.Permanent, "IT", "Head Office"),
            ("Diana Head (dev stub)", "diana.head@rms.local", UserRole.DepartmentHead,
                "Manager", "Department Head", EmploymentType.Permanent, "IT", "Head Office"),
            // Feature 5: two of them, deliberately - rule 1 requires every Procurement Officer to see
            // every Approved requisition with no per-officer assignment, and the easiest way to prove
            // that live is to have two and confirm both see an identical, unfiltered queue.
            ("Pat Procurement (dev stub)", "pat.procurement@rms.local", UserRole.ProcurementOfficer,
                "Officer", "Procurement Officer", EmploymentType.Permanent, "Procurement", "Head Office"),
            ("Noah Procurement (dev stub)", "noah.procurement@rms.local", UserRole.ProcurementOfficer,
                "Officer", "Procurement Officer", EmploymentType.Permanent, "Procurement", "Head Office"),
            // Feature 6: HrManager is an existing UserRole that's never been routed on before - the
            // Manpower Requisition approval workflow is Admin-configured (not hardcoded) but a
            // sensible default is "HR Review", assigned to HrManager rather than DepartmentHead,
            // since a Department Head submitting a manpower request must never end up approving
            // their own submission.
            ("Hana HR (dev stub)", "hana.hr@rms.local", UserRole.HrManager,
                "Manager", "HR Manager", EmploymentType.Permanent, "HR", "Head Office"),
        };

        foreach (var (fullName, email, role, grade, designation, employmentType, department, location) in demoActors)
        {
            var exists = await context.Users.AnyAsync(u => u.Email == email);
            if (!exists)
            {
                context.Users.Add(new User
                {
                    CompanyId = company.Id,
                    FullName = fullName,
                    Email = email,
                    Role = role,
                    Grade = grade,
                    Designation = designation,
                    EmploymentType = employmentType,
                    Department = department,
                    Location = location,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
