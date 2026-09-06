using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEligibilityPolicyManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentType",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "CategoryItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EligibilityPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityPolicies_CategoryItems_CategoryItemId",
                        column: x => x.CategoryItemId,
                        principalTable: "CategoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EligibilityPolicies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EligibilityPolicies_RequisitionCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "RequisitionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityPolicyCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EligibilityPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionType = table.Column<int>(type: "integer", nullable: false),
                    AllowedValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityPolicyCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityPolicyCriteria_EligibilityPolicies_EligibilityPo~",
                        column: x => x.EligibilityPolicyId,
                        principalTable: "EligibilityPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityPolicyReplacementRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EligibilityPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationValue = table.Column<int>(type: "integer", nullable: false),
                    DurationUnit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityPolicyReplacementRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityPolicyReplacementRules_EligibilityPolicies_Eligi~",
                        column: x => x.EligibilityPolicyId,
                        principalTable: "EligibilityPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityPolicies_CategoryId",
                table: "EligibilityPolicies",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityPolicies_CategoryItemId",
                table: "EligibilityPolicies",
                column: "CategoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityPolicies_CompanyId_CategoryId_CategoryItemId",
                table: "EligibilityPolicies",
                columns: new[] { "CompanyId", "CategoryId", "CategoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityPolicyCriteria_EligibilityPolicyId",
                table: "EligibilityPolicyCriteria",
                column: "EligibilityPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityPolicyReplacementRules_EligibilityPolicyId",
                table: "EligibilityPolicyReplacementRules",
                column: "EligibilityPolicyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EligibilityPolicyCriteria");

            migrationBuilder.DropTable(
                name: "EligibilityPolicyReplacementRules");

            migrationBuilder.DropTable(
                name: "EligibilityPolicies");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "CategoryItems");
        }
    }
}
