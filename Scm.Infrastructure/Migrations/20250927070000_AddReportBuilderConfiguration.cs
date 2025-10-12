using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scm.Infrastructure.Migrations
{
    public partial class AddReportBuilderConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuilderConfigurationJson",
                table: "ReportDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "{}");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuilderConfigurationJson",
                table: "ReportDefinitions");
        }
    }
}
