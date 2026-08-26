using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonTrashLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurgeJobId",
                table: "LessonConfig",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurgeStartedAt",
                table: "LessonConfig",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionQuestionReviewExclusion",
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
                    SessionQuestionId = table.Column<string>(type: "text", nullable: false),
                    LessonId = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionQuestionReviewExclusion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonConfig_CompanyId_IsDelete_DeletedAt",
                table: "LessonConfig",
                columns: new[] { "CompanyId", "IsDelete", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionQuestionReviewExclusion_CompanyId_LessonId",
                table: "SessionQuestionReviewExclusion",
                columns: new[] { "CompanyId", "LessonId" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionQuestionReviewExclusion_CompanyId_SessionQuestionId",
                table: "SessionQuestionReviewExclusion",
                columns: new[] { "CompanyId", "SessionQuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionQuestionReviewExclusion");

            migrationBuilder.DropIndex(
                name: "IX_LessonConfig_CompanyId_IsDelete_DeletedAt",
                table: "LessonConfig");

            migrationBuilder.DropColumn(
                name: "PurgeJobId",
                table: "LessonConfig");

            migrationBuilder.DropColumn(
                name: "PurgeStartedAt",
                table: "LessonConfig");
        }
    }
}
