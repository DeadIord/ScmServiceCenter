using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicianTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TechnicianTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedUserId = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicianTasks_AspNetUsers_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TechnicianTasks_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTasks_AssignedUserId",
                table: "TechnicianTasks",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTasks_OrderId",
                table: "TechnicianTasks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTasks_Status",
                table: "TechnicianTasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnicianTasks");
        }
    }
}
