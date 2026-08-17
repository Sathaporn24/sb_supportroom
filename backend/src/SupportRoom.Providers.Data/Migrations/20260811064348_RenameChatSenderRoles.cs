using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <summary>
    /// Data-only migration - no schema change. ChatMessage.SenderRole is a plain string column, so
    /// renaming the accepted values ("teacher" -> "recipient", "cs" -> "agent") produces no model
    /// diff and the scaffolder generated an empty migration; the UPDATEs below are written by hand.
    ///
    /// Without this, chat history written before the rename keeps the old values. Nothing crashes -
    /// the frontend looks the role up in a label map - but every one of those messages renders with
    /// a blank sender, which reads as a rendering bug rather than as old data.
    /// </summary>
    public partial class RenameChatSenderRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "ChatMessage" SET "SenderRole" = 'recipient' WHERE "SenderRole" = 'teacher';""");
            migrationBuilder.Sql("""UPDATE "ChatMessage" SET "SenderRole" = 'agent' WHERE "SenderRole" = 'cs';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "ChatMessage" SET "SenderRole" = 'teacher' WHERE "SenderRole" = 'recipient';""");
            migrationBuilder.Sql("""UPDATE "ChatMessage" SET "SenderRole" = 'cs' WHERE "SenderRole" = 'agent';""");
        }
    }
}
