using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "RequisitionAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "RequisitionAttachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "RequisitionAttachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RequisitionAttachments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RequisitionAttachments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionAttachments_RequisitionId_DocumentType",
                table: "RequisitionAttachments",
                columns: new[] { "RequisitionId", "DocumentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequisitionAttachments_RequisitionId_DocumentType",
                table: "RequisitionAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "RequisitionAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RequisitionAttachments");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "RequisitionAttachments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RequisitionAttachments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RequisitionAttachments");
        }
    }
}
