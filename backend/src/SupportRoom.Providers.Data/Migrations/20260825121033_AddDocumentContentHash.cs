using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentContentHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "DocumentResource",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentResource_CompanyId_ContentHash",
                table: "DocumentResource",
                columns: new[] { "CompanyId", "ContentHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentResource_CompanyId_ContentHash",
                table: "DocumentResource");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "DocumentResource");
        }
    }
}
