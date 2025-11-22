using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAssignedUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedUserId",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AssignedUserId",
                table: "Orders",
                column: "AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_AssignedUserId",
                table: "Orders",
                column: "AssignedUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_AssignedUserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AssignedUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "Orders");
        }
    }
}
