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
    }
}
