using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMS_DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddContentToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Documents",
                type: "text",
                nullable: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Documents");
        }
    }
}
