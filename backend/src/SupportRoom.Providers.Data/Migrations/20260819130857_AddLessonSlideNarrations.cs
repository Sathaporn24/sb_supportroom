using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonSlideNarrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonSlideNarration",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    CreateBy = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateBy = table.Column<string>(type: "text", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleteBy = table.Column<string>(type: "text", nullable: true),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LessonId = table.Column<string>(type: "text", nullable: false),
                    SlideObjectId = table.Column<string>(type: "text", nullable: false),
                    NarrationText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSlideNarration", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSlideNarration_CompanyId",
                table: "LessonSlideNarration",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSlideNarration_LessonId_SlideObjectId",
                table: "LessonSlideNarration",
                columns: new[] { "LessonId", "SlideObjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonSlideNarration");
        }
    }
}
