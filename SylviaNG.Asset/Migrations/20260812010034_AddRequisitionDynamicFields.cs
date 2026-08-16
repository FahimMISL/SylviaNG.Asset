using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequisitionDynamicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryVersionNumber",
                table: "Requisitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "Requisitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectCode",
                table: "Requisitions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequisitionFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionFieldValues_CategoryFieldDefinitions_FieldDefini~",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CategoryFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisitionFieldValues_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_CostCenterId",
                table: "Requisitions",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionFieldValues_FieldDefinitionId",
                table: "RequisitionFieldValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionFieldValues_RequisitionId",
                table: "RequisitionFieldValues",
                column: "RequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_CostCenters_CostCenterId",
                table: "Requisitions",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_CostCenters_CostCenterId",
                table: "Requisitions");

            migrationBuilder.DropTable(
                name: "RequisitionFieldValues");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_CostCenterId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "CategoryVersionNumber",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "ProjectCode",
                table: "Requisitions");
        }
    }
}
