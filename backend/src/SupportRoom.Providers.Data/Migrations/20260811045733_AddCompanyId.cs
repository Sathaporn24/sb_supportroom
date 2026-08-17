using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonConfig_Slug",
                table: "LessonConfig");

            // Hand-corrected: the scaffolder paired these two renames the wrong way round
            // (TeacherName -> RecipientOrgName, SchoolName -> RecipientName). It matches renamed
            // columns positionally, not by meaning, and both are nullable text so nothing about
            // the types made the mismatch visible. Left as generated it would have silently
            // swapped every existing row's person name and organization name.
            migrationBuilder.RenameColumn(
                name: "TeacherName",
                table: "TrainingSession",
                newName: "RecipientName");

            migrationBuilder.RenameColumn(
                name: "SchoolName",
                table: "TrainingSession",
                newName: "RecipientOrgName");

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "TrainingSession",
                type: "text",
                nullable: false,
                // Backfills existing rows to the same company DEFAULT_COMPANY_ID resolves to.
                // The scaffolder generated "" here, which no request ever resolves to - every
                // pre-existing row would have been filtered out of every query and looked deleted.
                defaultValue: "default");

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "SessionSummary",
                type: "text",
                nullable: false,
                // Backfills existing rows to the same company DEFAULT_COMPANY_ID resolves to.
                // The scaffolder generated "" here, which no request ever resolves to - every
                // pre-existing row would have been filtered out of every query and looked deleted.
                defaultValue: "default");

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "SessionQuestion",
                type: "text",
                nullable: false,
                // Backfills existing rows to the same company DEFAULT_COMPANY_ID resolves to.
                // The scaffolder generated "" here, which no request ever resolves to - every
                // pre-existing row would have been filtered out of every query and looked deleted.
                defaultValue: "default");

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "LessonConfig",
                type: "text",
                nullable: false,
                // Backfills existing rows to the same company DEFAULT_COMPANY_ID resolves to.
                // The scaffolder generated "" here, which no request ever resolves to - every
                // pre-existing row would have been filtered out of every query and looked deleted.
                defaultValue: "default");

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "DocumentResource",
                type: "text",
                nullable: false,
                // Backfills existing rows to the same company DEFAULT_COMPANY_ID resolves to.
                // The scaffolder generated "" here, which no request ever resolves to - every
                // pre-existing row would have been filtered out of every query and looked deleted.
                defaultValue: "default");

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "ChatMessage",
                type: "text",
                nullable: false,
                // Backfills existing rows to the same company DEFAULT_COMPANY_ID resolves to.
                // The scaffolder generated "" here, which no request ever resolves to - every
                // pre-existing row would have been filtered out of every query and looked deleted.
                defaultValue: "default");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSession_CompanyId",
                table: "TrainingSession",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionSummary_CompanyId",
                table: "SessionSummary",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionQuestion_CompanyId",
                table: "SessionQuestion",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonConfig_CompanyId_Slug",
                table: "LessonConfig",
                columns: new[] { "CompanyId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentResource_CompanyId",
                table: "DocumentResource",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_CompanyId",
                table: "ChatMessage",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingSession_CompanyId",
                table: "TrainingSession");

            migrationBuilder.DropIndex(
                name: "IX_SessionSummary_CompanyId",
                table: "SessionSummary");

            migrationBuilder.DropIndex(
                name: "IX_SessionQuestion_CompanyId",
                table: "SessionQuestion");

            migrationBuilder.DropIndex(
                name: "IX_LessonConfig_CompanyId_Slug",
                table: "LessonConfig");

            migrationBuilder.DropIndex(
                name: "IX_DocumentResource_CompanyId",
                table: "DocumentResource");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessage_CompanyId",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "TrainingSession");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SessionSummary");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SessionQuestion");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "LessonConfig");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DocumentResource");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ChatMessage");

            // Mirrors the hand-corrected pairing in Up().
            migrationBuilder.RenameColumn(
                name: "RecipientName",
                table: "TrainingSession",
                newName: "TeacherName");

            migrationBuilder.RenameColumn(
                name: "RecipientOrgName",
                table: "TrainingSession",
                newName: "SchoolName");

            migrationBuilder.CreateIndex(
                name: "IX_LessonConfig_Slug",
                table: "LessonConfig",
                column: "Slug",
                unique: true);
        }
    }
}
