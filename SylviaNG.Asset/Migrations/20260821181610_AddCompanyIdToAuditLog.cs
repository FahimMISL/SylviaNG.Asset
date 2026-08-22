using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId",
                table: "AuditLogs",
                column: "CompanyId");

            // One-time backfill for rows written before AuditLogger started capturing CompanyId
            // directly - resolved per EntityName, same set of audited entity types as today
            // (Requisition, RequisitionCategory, CostCenter, EligibilityPolicy, ApprovalWorkflow,
            // ApprovalWorkflowVersion, ApprovalDelegation), plus the legacy RequisitionApproval
            // anchoring used before the Feature 8 fix that re-anchored approval-decision events to
            // their requisition. Any row whose entity has since been deleted is left NULL here - it
            // was never visible in the audit view before this migration either, and with only one
            // company seeded in this environment it's a non-issue in practice; a genuinely multi-
            // company deployment with pre-existing history would need a one-off manual reconciliation.
            // The Feature 8 immutability trigger (previous migration) rejects UPDATE outright, including
            // this migration's own one-time backfill - disable it for just this backfill, then restore it.
            migrationBuilder.Sql("""ALTER TABLE "AuditLogs" DISABLE TRIGGER trg_audit_logs_no_update;""");

            migrationBuilder.Sql(
                """
                UPDATE "AuditLogs" a SET "CompanyId" = r."CompanyId"
                FROM "Requisitions" r WHERE a."EntityName" = 'Requisition' AND a."EntityId" = r."Id";

                UPDATE "AuditLogs" a SET "CompanyId" = c."CompanyId"
                FROM "RequisitionCategories" c WHERE a."EntityName" = 'RequisitionCategory' AND a."EntityId" = c."Id";

                UPDATE "AuditLogs" a SET "CompanyId" = c."CompanyId"
                FROM "CostCenters" c WHERE a."EntityName" = 'CostCenter' AND a."EntityId" = c."Id";

                UPDATE "AuditLogs" a SET "CompanyId" = p."CompanyId"
                FROM "EligibilityPolicies" p WHERE a."EntityName" = 'EligibilityPolicy' AND a."EntityId" = p."Id";

                UPDATE "AuditLogs" a SET "CompanyId" = w."CompanyId"
                FROM "ApprovalWorkflows" w WHERE a."EntityName" = 'ApprovalWorkflow' AND a."EntityId" = w."Id";

                UPDATE "AuditLogs" a SET "CompanyId" = w."CompanyId"
                FROM "ApprovalWorkflowVersions" v JOIN "ApprovalWorkflows" w ON v."ApprovalWorkflowId" = w."Id"
                WHERE a."EntityName" = 'ApprovalWorkflowVersion' AND a."EntityId" = v."Id";

                UPDATE "AuditLogs" a SET "CompanyId" = d."CompanyId"
                FROM "ApprovalDelegations" d WHERE a."EntityName" = 'ApprovalDelegation' AND a."EntityId" = d."Id";

                UPDATE "AuditLogs" a SET "CompanyId" = r."CompanyId"
                FROM "RequisitionApprovals" ra
                JOIN "RequisitionApprovalProcesses" p ON ra."RequisitionApprovalProcessId" = p."Id"
                JOIN "Requisitions" r ON p."RequisitionId" = r."Id"
                WHERE a."EntityName" = 'RequisitionApproval' AND a."EntityId" = ra."Id";
                """);

            migrationBuilder.Sql("""ALTER TABLE "AuditLogs" ENABLE TRIGGER trg_audit_logs_no_update;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CompanyId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AuditLogs");
        }
    }
}
