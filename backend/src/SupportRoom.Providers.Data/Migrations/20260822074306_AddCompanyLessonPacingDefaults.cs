using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyLessonPacingDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Widening NOT NULL -> NULL keeps every existing row's value exactly as it was (P4) -
            // no UPDATE needed here, every lesson that had a value becomes an explicit override of
            // that same value, not an accidental "inherit".
            migrationBuilder.AlterColumn<int>(
                name: "IntroWaitMs",
                table: "LessonConfig",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FinalQuestionWaitMs",
                table: "LessonConfig",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "BreathPauseMs",
                table: "LessonConfig",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // R-11 - these three literals (5000/500/5000) are TutorConfig.DefaultIntroWaitMs/
            // DefaultBreathPauseMs/DefaultFinalQuestionWaitMs, copied by hand rather than read from
            // ServerDefaults.GetLessonTimingDefaults(): `dotnet ef database update` runs in a
            // process with its own environment, not the app's, so reading DEFAULT_*_MS here would
            // make the backfilled value depend on which machine happens to run the migration - not
            // deterministic. If the deployment target's env vars differ from these literals,
            // devops must run a one-off `UPDATE "Company" SET ...` right after this migration
            // (see design.md's Migration Plan for Module P, point 2) - that is intentionally not
            // this migration's job.
            migrationBuilder.AddColumn<int>(
                name: "DefaultBreathPauseMs",
                table: "Company",
                type: "integer",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddColumn<int>(
                name: "DefaultFinalQuestionWaitMs",
                table: "Company",
                type: "integer",
                nullable: false,
                defaultValue: 5000);

            migrationBuilder.AddColumn<int>(
                name: "DefaultIntroWaitMs",
                table: "Company",
                type: "integer",
                nullable: false,
                defaultValue: 5000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Asymmetric rollback (design.md's Migration Plan for Module P, point 3): once any
            // lesson has been saved with a null (inherit) pacing value, reverting to NOT NULL
            // cannot just invent a number - defaulting every null row to 0 would silently turn
            // "this lesson inherits the company's pacing" into an explicit "wait 0ms" override,
            // which is not what a rollback should do. Instead, backfill each null row from the
            // Company row it currently inherits from - the exact value it was using a moment ago -
            // BEFORE the Company columns are dropped below, since this UPDATE needs to read them.
            migrationBuilder.Sql("""
                UPDATE "LessonConfig" lesson
                SET "IntroWaitMs" = COALESCE(lesson."IntroWaitMs", company."DefaultIntroWaitMs"),
                    "BreathPauseMs" = COALESCE(lesson."BreathPauseMs", company."DefaultBreathPauseMs"),
                    "FinalQuestionWaitMs" = COALESCE(lesson."FinalQuestionWaitMs", company."DefaultFinalQuestionWaitMs")
                FROM "Company" company
                WHERE lesson."CompanyId" = company."Id";
                """);

            migrationBuilder.DropColumn(
                name: "DefaultBreathPauseMs",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "DefaultFinalQuestionWaitMs",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "DefaultIntroWaitMs",
                table: "Company");

            migrationBuilder.AlterColumn<int>(
                name: "IntroWaitMs",
                table: "LessonConfig",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FinalQuestionWaitMs",
                table: "LessonConfig",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BreathPauseMs",
                table: "LessonConfig",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
