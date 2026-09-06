using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurementAndFulfillmentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Requisitions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "FulfilledQuantity",
                table: "RequisitionItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RequisitionProcurementRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActorRole = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TotalProcurementAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionProcurementRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionProcurementRecords_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequisitionProcurementLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionProcurementRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    QuantityFulfilledThisAction = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionProcurementLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionProcurementLineItems_RequisitionItems_Requisitio~",
                        column: x => x.RequisitionItemId,
                        principalTable: "RequisitionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisitionProcurementLineItems_RequisitionProcurementRecor~",
                        column: x => x.RequisitionProcurementRecordId,
                        principalTable: "RequisitionProcurementRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionProcurementLineItems_RequisitionItemId",
                table: "RequisitionProcurementLineItems",
                column: "RequisitionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionProcurementLineItems_RequisitionProcurementRecor~",
                table: "RequisitionProcurementLineItems",
                column: "RequisitionProcurementRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionProcurementRecords_RequisitionId",
                table: "RequisitionProcurementRecords",
                column: "RequisitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequisitionProcurementLineItems");

            migrationBuilder.DropTable(
                name: "RequisitionProcurementRecords");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "FulfilledQuantity",
                table: "RequisitionItems");
        }
    }
}
