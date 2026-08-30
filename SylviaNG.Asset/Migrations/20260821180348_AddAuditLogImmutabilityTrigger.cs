using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogImmutabilityTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Feature 8 (US-026): append-only enforcement at the database level, not just app
            // convention - rejects any UPDATE/DELETE against AuditLogs outright, including one run
            // directly via psql as SystemAdmin/postgres, not just a call the app itself never makes.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION reject_audit_log_mutation()
                RETURNS TRIGGER AS $$
                BEGIN
                    RAISE EXCEPTION 'AuditLogs is append-only: % is not permitted.', TG_OP;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_audit_logs_no_update
                BEFORE UPDATE ON "AuditLogs"
                FOR EACH ROW EXECUTE FUNCTION reject_audit_log_mutation();

                CREATE TRIGGER trg_audit_logs_no_delete
                BEFORE DELETE ON "AuditLogs"
                FOR EACH ROW EXECUTE FUNCTION reject_audit_log_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_audit_logs_no_update ON "AuditLogs";
                DROP TRIGGER IF EXISTS trg_audit_logs_no_delete ON "AuditLogs";
                DROP FUNCTION IF EXISTS reject_audit_log_mutation();
                """);
        }
    }
}
