using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Module P, N1/N2/N3 (2026-08-22) - the project owner reversed the earlier P1 answer after
    /// seeing the real screen: lesson pacing is company-level only, with no per-lesson override at
    /// all (design.md's Lesson Pacing Resolution Rules, rewritten the same day). These three
    /// columns on LessonConfig existed to hold that override (added nullable by
    /// AddCompanyLessonPacingDefaults) and are no longer read or written by any code path -
    /// GetTeachingContentByLinkAsync now reads Company.Default*Ms directly.
    ///
    /// Whatever override values CS had already set on individual lessons are dropped here on
    /// purpose, not lost by accident - design.md's Migration Plan for Module P explicitly forbids
    /// an UPDATE that tries to preserve them (e.g. copying a lesson's old value into the company
    /// row) because that would silently let one lesson's override outlive the feature it belonged
    /// to. See design.md's Data Model, DM-P2, for the full reasoning.
    /// </summary>
    public partial class RemoveLessonConfigPacingOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntroWaitMs",
                table: "LessonConfig");

            migrationBuilder.DropColumn(
                name: "BreathPauseMs",
                table: "LessonConfig");

            migrationBuilder.DropColumn(
                name: "FinalQuestionWaitMs",
                table: "LessonConfig");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback restores the SHAPE only, not the DATA - the values dropped by Up() above are
            // gone for good (that was the intentional data loss described in the class comment).
            // Recreating these as nullable (their state right before this migration) rather than
            // guessing a value (0, or copying from Company) avoids turning a rollback into a second,
            // silent data-fabrication event.
            migrationBuilder.AddColumn<int>(
                name: "IntroWaitMs",
                table: "LessonConfig",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BreathPauseMs",
                table: "LessonConfig",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalQuestionWaitMs",
                table: "LessonConfig",
                type: "integer",
                nullable: true);
        }
    }
}
