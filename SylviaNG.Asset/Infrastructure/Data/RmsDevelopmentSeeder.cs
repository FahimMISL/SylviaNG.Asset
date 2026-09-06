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
            // Feature 7: Ceo is an existing UserRole that's never been routed on before - the
            // Executive Summary report is its first real use, needed to test CEO-only access.
            ("Chris CEO (dev stub)", "chris.ceo@rms.local", UserRole.Ceo,
                "Executive", "Chief Executive Officer", EmploymentType.Permanent, "Executive", "Head Office"),
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

        await SeedRolePermissionsAsync(context, company.Id);
    }

    /// <summary>
    /// Feature 10 (US-031): seeds the permission matrix to match what each role can ALREADY do today
    /// via the existing inline IsInRole checks (see the plan's Context) - so turning RBAC on doesn't
    /// change anyone's access on day one, it just makes it visible/editable. Only grants (IsAllowed=true)
    /// are listed - PermissionService fails closed for any (Role, Module, Action) with no row, so
    /// everything not listed here is correctly "not allowed" by omission. SystemAdmin has no rows at
    /// all - it implicitly passes every check (see PermissionService), so seeding its row would be
    /// inert. Idempotent: only inserts combinations that don't already exist.
    /// </summary>
    private static async Task SeedRolePermissionsAsync(RmsDbContext context, Guid companyId)
    {
        if (await context.RolePermissions.AnyAsync(p => p.CompanyId == companyId))
        {
            return;
        }

        static (UserRole, PermissionModule, PermissionAction)[] Grants(UserRole role, PermissionModule module, params PermissionAction[] actions) =>
            actions.Select(a => (role, module, a)).ToArray();

        var grants = new List<(UserRole Role, PermissionModule Module, PermissionAction Action)>();
        grants.AddRange(Grants(UserRole.Employee, PermissionModule.RequisitionSetup, PermissionAction.View));
        grants.AddRange(Grants(UserRole.Employee, PermissionModule.RequisitionRequests, PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Delete));
        grants.AddRange(Grants(UserRole.Employee, PermissionModule.Notifications, PermissionAction.View, PermissionAction.Edit));
        grants.AddRange(Grants(UserRole.Employee, PermissionModule.Dashboard, PermissionAction.View));
        grants.AddRange(Grants(UserRole.Employee, PermissionModule.Search, PermissionAction.View));

        grants.AddRange(Grants(UserRole.LineManager, PermissionModule.RequisitionSetup, PermissionAction.View));
        grants.AddRange(Grants(UserRole.LineManager, PermissionModule.RequisitionRequests, PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Approve, PermissionAction.Delete));
        grants.AddRange(Grants(UserRole.LineManager, PermissionModule.Notifications, PermissionAction.View, PermissionAction.Edit));
        grants.AddRange(Grants(UserRole.LineManager, PermissionModule.Dashboard, PermissionAction.View));
        grants.AddRange(Grants(UserRole.LineManager, PermissionModule.Search, PermissionAction.View));

        grants.AddRange(Grants(UserRole.DepartmentHead, PermissionModule.RequisitionSetup, PermissionAction.View));
        grants.AddRange(Grants(UserRole.DepartmentHead, PermissionModule.RequisitionRequests, PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Approve, PermissionAction.Delete));
        grants.AddRange(Grants(UserRole.DepartmentHead, PermissionModule.Manpower, PermissionAction.View, PermissionAction.Create));
        grants.AddRange(Grants(UserRole.DepartmentHead, PermissionModule.Notifications, PermissionAction.View, PermissionAction.Edit));
        grants.AddRange(Grants(UserRole.DepartmentHead, PermissionModule.Dashboard, PermissionAction.View));
        grants.AddRange(Grants(UserRole.DepartmentHead, PermissionModule.Search, PermissionAction.View));

        grants.AddRange(Grants(UserRole.ProcurementOfficer, PermissionModule.RequisitionSetup, PermissionAction.View));
        grants.AddRange(Grants(UserRole.ProcurementOfficer, PermissionModule.RequisitionRequests, PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Delete));
        grants.AddRange(Grants(UserRole.ProcurementOfficer, PermissionModule.Procurement, PermissionAction.View, PermissionAction.Create, PermissionAction.Edit));
        grants.AddRange(Grants(UserRole.ProcurementOfficer, PermissionModule.Notifications, PermissionAction.View, PermissionAction.Edit));
        grants.AddRange(Grants(UserRole.ProcurementOfficer, PermissionModule.Dashboard, PermissionAction.View));
        grants.AddRange(Grants(UserRole.ProcurementOfficer, PermissionModule.Search, PermissionAction.View));

        grants.AddRange(Grants(UserRole.HrManager, PermissionModule.RequisitionSetup, PermissionAction.View));
        grants.AddRange(Grants(UserRole.HrManager, PermissionModule.RequisitionRequests, PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Delete));
        grants.AddRange(Grants(UserRole.HrManager, PermissionModule.Manpower, PermissionAction.View, PermissionAction.Approve));
        grants.AddRange(Grants(UserRole.HrManager, PermissionModule.Notifications, PermissionAction.View, PermissionAction.Edit));
        grants.AddRange(Grants(UserRole.HrManager, PermissionModule.Dashboard, PermissionAction.View));
        grants.AddRange(Grants(UserRole.HrManager, PermissionModule.Search, PermissionAction.View));

        grants.AddRange(Grants(UserRole.Ceo, PermissionModule.RequisitionSetup, PermissionAction.View));
        grants.AddRange(Grants(UserRole.Ceo, PermissionModule.RequisitionRequests, PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Delete));
        grants.AddRange(Grants(UserRole.Ceo, PermissionModule.Reporting, PermissionAction.View));
        grants.AddRange(Grants(UserRole.Ceo, PermissionModule.Notifications, PermissionAction.View, PermissionAction.Edit));
        grants.AddRange(Grants(UserRole.Ceo, PermissionModule.Dashboard, PermissionAction.View));
        grants.AddRange(Grants(UserRole.Ceo, PermissionModule.Search, PermissionAction.View));

        foreach (var (role, module, action) in grants)
        {
            context.RolePermissions.Add(new RolePermission
            {
                CompanyId = companyId,
                Role = role,
                Module = module,
                Action = action,
                IsAllowed = true,
            });
        }

        await context.SaveChangesAsync();
    }
}
