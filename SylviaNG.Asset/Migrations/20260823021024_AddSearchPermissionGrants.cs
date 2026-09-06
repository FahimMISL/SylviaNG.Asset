using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchPermissionGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Feature 11: PermissionModule.Search=11/PermissionAction.View=0 is a new enum value, not a
            // schema change - RolePermissions already has room for it. RmsDevelopmentSeeder's own
            // idempotent early-return ("skip if this company already has any RolePermissions rows")
            // means a company seeded before this feature existed will never pick up the new Search
            // grant just by re-running the seeder, so this migration backfills it directly for every
            // company that already has RolePermissions rows today. UserRole values: Employee=0,
            // LineManager=1, DepartmentHead=2, ProcurementOfficer=4, HrManager=5, Ceo=8 (SystemAdmin=7
            // excluded - it bypasses the table entirely, same as every other module).
            migrationBuilder.Sql(@"
                INSERT INTO ""RolePermissions"" (""Id"", ""CompanyId"", ""Role"", ""Module"", ""Action"", ""IsAllowed"")
                SELECT gen_random_uuid(), c.""CompanyId"", r.role, 11, 0, true
                FROM (SELECT DISTINCT ""CompanyId"" FROM ""RolePermissions"") c
                CROSS JOIN (VALUES (0), (1), (2), (4), (5), (8)) AS r(role)
                ON CONFLICT (""CompanyId"", ""Role"", ""Module"", ""Action"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""Module"" = 11;");
        }
    }
}
