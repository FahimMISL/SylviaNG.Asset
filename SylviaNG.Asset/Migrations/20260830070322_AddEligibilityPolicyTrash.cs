using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEligibilityPolicyTrash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "RequisitionItems",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "EligibilityPolicies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "EligibilityPolicies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EligibilityPolicies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityPolicies_IsDeleted_DeletedAtUtc",
                table: "EligibilityPolicies",
                columns: new[] { "IsDeleted", "DeletedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EligibilityPolicies_IsDeleted_DeletedAtUtc",
                table: "EligibilityPolicies");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "EligibilityPolicies");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EligibilityPolicies");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EligibilityPolicies");
        }
    }
}
