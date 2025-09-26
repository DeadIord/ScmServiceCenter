using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "At",
                table: "Messages",
                newName: "AtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_At",
                table: "Messages",
                newName: "IX_Messages_AtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AtUtc",
                table: "Messages",
                newName: "At");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_AtUtc",
                table: "Messages",
                newName: "IX_Messages_At");
        }
    }
}
