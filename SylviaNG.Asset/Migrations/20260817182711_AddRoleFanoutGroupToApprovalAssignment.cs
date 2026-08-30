using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleFanoutGroupToApprovalAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoleFanoutGroupId",
                table: "RequisitionApprovalAssignments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleFanoutGroupId",
                table: "RequisitionApprovalAssignments");
        }
    }
}
