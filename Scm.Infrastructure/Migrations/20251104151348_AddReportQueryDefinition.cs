using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportQueryDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QueryDefinitionJson",
                table: "ReportDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QueryDefinitionJson",
                table: "ReportDefinitions");
        }
    }
}
