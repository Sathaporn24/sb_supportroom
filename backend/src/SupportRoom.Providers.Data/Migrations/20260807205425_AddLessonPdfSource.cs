using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonPdfSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentSourceType",
                table: "LessonConfig",
                type: "text",
                nullable: false,
                defaultValue: "google_slides");

            migrationBuilder.AddColumn<string>(
                name: "PdfDocumentResourceId",
                table: "LessonConfig",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentSourceType",
                table: "LessonConfig");

            migrationBuilder.DropColumn(
                name: "PdfDocumentResourceId",
                table: "LessonConfig");
        }
    }
}
