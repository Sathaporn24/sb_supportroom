using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// F10/F10-a, U2/U3 (2026-08-23) - one migration carries both halves of the same feature drop
    /// (design.md's MG-R1): learner typed questions ship in the same release as the removal of the
    /// CS chat feature, so there is no deploy state where only one half is live.
    /// </summary>
    public partial class RemoveChatMessageAndAddQuestionSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // U2 - SessionQuestion.Source. Backfilled via defaultValue, not a separate UPDATE:
            // every row that exists as of this migration was asked by voice, as a matter of fact -
            // typing a question does not exist in the product until this same release ships. The
            // default is then dropped immediately below so any future write that forgets to set
            // Source fails at compile time (the entity property is `required`) rather than silently
            // getting "voice" it never asked for.
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "SessionQuestion",
                type: "text",
                nullable: false,
                defaultValue: "voice");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "SessionQuestion",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "voice");

            // U3/F10-a - the CS "chat during a lesson" feature is cut entirely, not migrated
            // elsewhere. Every message that ever existed in this table is deleted here on purpose:
            // it was a live conversation channel, not content anyone needs to read back later, and
            // design.md explicitly forbids archiving it. The F7 plan that once said "re-point
            // ChatMessage.SessionId at LearningSession" is void - there is no ChatMessage anymore.
            migrationBuilder.DropTable(
                name: "ChatMessage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the ChatMessage TABLE SHAPE only - the rows Up() deleted above are gone for
            // good, by design. Anyone running this rollback gets an empty table, not the
            // conversation history that existed before the migration.
            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: false),
                    CreateBy = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeleteBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    SenderName = table.Column<string>(type: "text", nullable: true),
                    SenderRole = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    UpdateBy = table.Column<string>(type: "text", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_CompanyId",
                table: "ChatMessage",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SessionId",
                table: "ChatMessage",
                column: "SessionId");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "SessionQuestion");
        }
    }
}
