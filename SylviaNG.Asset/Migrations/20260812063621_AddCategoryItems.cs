using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryItemId",
                table: "RequisitionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryItems_RequisitionCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "RequisitionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionItems_CategoryItemId",
                table: "RequisitionItems",
                column: "CategoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryItems_CategoryId",
                table: "CategoryItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionItems_CategoryItems_CategoryItemId",
                table: "RequisitionItems",
                column: "CategoryItemId",
                principalTable: "CategoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionItems_CategoryItems_CategoryItemId",
                table: "RequisitionItems");

            migrationBuilder.DropTable(
                name: "CategoryItems");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionItems_CategoryItemId",
                table: "RequisitionItems");

            migrationBuilder.DropColumn(
                name: "CategoryItemId",
                table: "RequisitionItems");
        }
    }
}
