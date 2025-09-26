using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportBuilderV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SqlText = table.Column<string>(type: "text", nullable: false),
                    ParametersJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    AllowedRolesJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportExecutionLogs_ReportDefinitions_ReportId",
                        column: x => x.ReportId,
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_CreatedBy",
                table: "ReportDefinitions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_IsActive",
                table: "ReportDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_Visibility",
                table: "ReportDefinitions",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutionLogs_ReportId",
                table: "ReportExecutionLogs",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutionLogs_Status",
                table: "ReportExecutionLogs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportExecutionLogs");

            migrationBuilder.DropTable(
                name: "ReportDefinitions");
        }
    }
}
