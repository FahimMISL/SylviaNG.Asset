using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalWorkflowManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalDelegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDelegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalDelegations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalDelegations_Users_DelegateUserId",
                        column: x => x.DelegateUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalDelegations_Users_DelegatorUserId",
                        column: x => x.DelegatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflows_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    RoutingMode = table.Column<int>(type: "integer", nullable: false),
                    AppliesToAllCategories = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowVersions_ApprovalWorkflows_ApprovalWorkflow~",
                        column: x => x.ApprovalWorkflowId,
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowCategoryLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionCategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowCategoryLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowCategoryLinks_ApprovalWorkflowVersions_Appr~",
                        column: x => x.ApprovalWorkflowVersionId,
                        principalTable: "ApprovalWorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowCategoryLinks_RequisitionCategories_Requisi~",
                        column: x => x.RequisitionCategoryId,
                        principalTable: "RequisitionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageOrder = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CapturesEstimatedCost = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowStages_ApprovalWorkflowVersions_ApprovalWor~",
                        column: x => x.ApprovalWorkflowVersionId,
                        principalTable: "ApprovalWorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequisitionApprovalProcesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentStageOrder = table.Column<int>(type: "integer", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionApprovalProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionApprovalProcesses_ApprovalWorkflowVersions_Appro~",
                        column: x => x.ApprovalWorkflowVersionId,
                        principalTable: "ApprovalWorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisitionApprovalProcesses_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowSlaConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationValue = table.Column<int>(type: "integer", nullable: false),
                    DurationUnit = table.Column<int>(type: "integer", nullable: false),
                    Reminder50PercentEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Reminder80PercentEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EscalateOnBreach = table.Column<bool>(type: "boolean", nullable: false),
                    EscalationApproverRole = table.Column<int>(type: "integer", nullable: true),
                    EscalationApproverUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowSlaConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowSlaConfigurations_ApprovalWorkflowStages_Ap~",
                        column: x => x.ApprovalWorkflowStageId,
                        principalTable: "ApprovalWorkflowStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowSlaConfigurations_Users_EscalationApproverU~",
                        column: x => x.EscalationApproverUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowStageConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConditionType = table.Column<int>(type: "integer", nullable: false),
                    MinCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaxCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowStageConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowStageConditions_ApprovalWorkflowStages_Appr~",
                        column: x => x.ApprovalWorkflowStageId,
                        principalTable: "ApprovalWorkflowStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowStageConditions_RequisitionCategories_Categ~",
                        column: x => x.CategoryId,
                        principalTable: "RequisitionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowApprovers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverType = table.Column<int>(type: "integer", nullable: false),
                    ApproverRole = table.Column<int>(type: "integer", nullable: true),
                    ApproverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FallbackApproverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowApprovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowApprovers_ApprovalWorkflowStages_ApprovalWorkflowSt~",
                        column: x => x.ApprovalWorkflowStageId,
                        principalTable: "ApprovalWorkflowStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowApprovers_Users_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowApprovers_Users_FallbackApproverUserId",
                        column: x => x.FallbackApproverUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequisitionApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionApprovalProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SlaStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SlaDueUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SlaPausedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SlaPausedDurationTotal = table.Column<TimeSpan>(type: "interval", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionApprovals_ApprovalWorkflowStages_ApprovalWorkflo~",
                        column: x => x.ApprovalWorkflowStageId,
                        principalTable: "ApprovalWorkflowStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisitionApprovals_RequisitionApprovalProcesses_Requisiti~",
                        column: x => x.RequisitionApprovalProcessId,
                        principalTable: "RequisitionApprovalProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequisitionApprovalActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActorRole = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DelegatedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EscalatedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CapturedEstimatedCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionApprovalActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionApprovalActions_RequisitionApprovals_Requisition~",
                        column: x => x.RequisitionApprovalId,
                        principalTable: "RequisitionApprovals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequisitionApprovalAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalApproverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    HasActed = table.Column<bool>(type: "boolean", nullable: false),
                    ActedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionApprovalAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionApprovalAssignments_RequisitionApprovals_Requisi~",
                        column: x => x.RequisitionApprovalId,
                        principalTable: "RequisitionApprovals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequisitionApprovalAssignments_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartialApprovalDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionApprovalActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedQuantity = table.Column<int>(type: "integer", nullable: false),
                    DeclinedQuantity = table.Column<int>(type: "integer", nullable: false),
                    DeclineReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialApprovalDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartialApprovalDecisions_RequisitionApprovalActions_Requisi~",
                        column: x => x.RequisitionApprovalActionId,
                        principalTable: "RequisitionApprovalActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartialApprovalDecisions_RequisitionItems_RequisitionItemId",
                        column: x => x.RequisitionItemId,
                        principalTable: "RequisitionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_CompanyId",
                table: "ApprovalDelegations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_DelegateUserId",
                table: "ApprovalDelegations",
                column: "DelegateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_DelegatorUserId_StartDate_EndDate",
                table: "ApprovalDelegations",
                columns: new[] { "DelegatorUserId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowCategoryLinks_ApprovalWorkflowVersionId_Req~",
                table: "ApprovalWorkflowCategoryLinks",
                columns: new[] { "ApprovalWorkflowVersionId", "RequisitionCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowCategoryLinks_RequisitionCategoryId",
                table: "ApprovalWorkflowCategoryLinks",
                column: "RequisitionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_CompanyId_Name",
                table: "ApprovalWorkflows",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowSlaConfigurations_ApprovalWorkflowStageId",
                table: "ApprovalWorkflowSlaConfigurations",
                column: "ApprovalWorkflowStageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowSlaConfigurations_EscalationApproverUserId",
                table: "ApprovalWorkflowSlaConfigurations",
                column: "EscalationApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowStageConditions_ApprovalWorkflowStageId",
                table: "ApprovalWorkflowStageConditions",
                column: "ApprovalWorkflowStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowStageConditions_CategoryId",
                table: "ApprovalWorkflowStageConditions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowStages_ApprovalWorkflowVersionId_StageOrder",
                table: "ApprovalWorkflowStages",
                columns: new[] { "ApprovalWorkflowVersionId", "StageOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowVersions_ApprovalWorkflowId_VersionNumber",
                table: "ApprovalWorkflowVersions",
                columns: new[] { "ApprovalWorkflowId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartialApprovalDecisions_RequisitionApprovalActionId",
                table: "PartialApprovalDecisions",
                column: "RequisitionApprovalActionId");

            migrationBuilder.CreateIndex(
                name: "IX_PartialApprovalDecisions_RequisitionItemId",
                table: "PartialApprovalDecisions",
                column: "RequisitionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovalActions_RequisitionApprovalId",
                table: "RequisitionApprovalActions",
                column: "RequisitionApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovalAssignments_AssignedUserId",
                table: "RequisitionApprovalAssignments",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovalAssignments_RequisitionApprovalId",
                table: "RequisitionApprovalAssignments",
                column: "RequisitionApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovalProcesses_ApprovalWorkflowVersionId",
                table: "RequisitionApprovalProcesses",
                column: "ApprovalWorkflowVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovalProcesses_RequisitionId",
                table: "RequisitionApprovalProcesses",
                column: "RequisitionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_ApprovalWorkflowStageId",
                table: "RequisitionApprovals",
                column: "ApprovalWorkflowStageId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionApprovals_RequisitionApprovalProcessId_StageOrder",
                table: "RequisitionApprovals",
                columns: new[] { "RequisitionApprovalProcessId", "StageOrder" },
                unique: true,
                filter: "\"Status\" IN (0, 1, 6)");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowApprovers_ApprovalWorkflowStageId",
                table: "WorkflowApprovers",
                column: "ApprovalWorkflowStageId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowApprovers_ApproverUserId",
                table: "WorkflowApprovers",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowApprovers_FallbackApproverUserId",
                table: "WorkflowApprovers",
                column: "FallbackApproverUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalDelegations");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowCategoryLinks");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowSlaConfigurations");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowStageConditions");

            migrationBuilder.DropTable(
                name: "PartialApprovalDecisions");

            migrationBuilder.DropTable(
                name: "RequisitionApprovalAssignments");

            migrationBuilder.DropTable(
                name: "WorkflowApprovers");

            migrationBuilder.DropTable(
                name: "RequisitionApprovalActions");

            migrationBuilder.DropTable(
                name: "RequisitionApprovals");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowStages");

            migrationBuilder.DropTable(
                name: "RequisitionApprovalProcesses");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowVersions");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflows");
        }
    }
}
