using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <summary>
    /// TD-013 (split "link" from "learning") and TD-014 (back-office authentication) in one
    /// migration, because neither has been deployed yet and two migrations would only mean two
    /// chances to run them out of order.
    ///
    /// ⚠️ HAND-EDITED. `dotnet ef migrations add` scaffolded DROP TrainingSession + CREATE
    /// TrainingLink, which destroys every existing link and leaves SessionQuestion.SessionId and
    /// ChatMessage.SessionId pointing at rows that no longer exist. This version renames the table
    /// so its data survives, splits each old row's learning half into LearningSession, and
    /// repoints the children - see the numbered steps in Up().
    ///
    /// If this is regenerated, the data steps must be re-applied by hand.
    /// </summary>
    public partial class SplitLinkAndAddAuth : Migration
    {
        /// <summary>
        /// New LearningSession ids are derived from the old TrainingSession id rather than being
        /// random, so the UPDATE that repoints SessionQuestion/ChatMessage is a plain string
        /// concatenation and needs no lookup table.
        /// </summary>
        private const string LegacyLearningPrefix = "learning_";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Company: the tenant registry, seeded from the company ids already in use ────
            // Without this, every existing row would reference a company that does not exist and
            // the switcher would have nothing to offer.
            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreateBy = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateBy = table.Column<string>(type: "text", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleteBy = table.Column<string>(type: "text", nullable: true),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Company", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_Company_IsActive", table: "Company", column: "IsActive");

            // Name defaults to the id - a human renames it afterwards. Guessing a display name
            // would be worse than showing the slug.
            migrationBuilder.Sql($"""
                INSERT INTO "Company" ("Id", "Name", "IsActive", "CreateDate", "IsDelete")
                SELECT DISTINCT c, c, true, now(), false
                FROM (
                    SELECT "CompanyId" AS c FROM "TrainingSession"
                    UNION SELECT "CompanyId" FROM "LessonConfig"
                    UNION SELECT "CompanyId" FROM "SessionQuestion"
                    UNION SELECT "CompanyId" FROM "ChatMessage"
                    UNION SELECT "CompanyId" FROM "DocumentResource"
                ) AS used
                WHERE c IS NOT NULL AND c <> '';
                """);

            // ── 2. AdminUser: created empty; the first owner is seeded at startup ─────────────
            migrationBuilder.CreateTable(
                name: "AdminUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    CreateBy = table.Column<string>(type: "text", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateBy = table.Column<string>(type: "text", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleteBy = table.Column<string>(type: "text", nullable: true),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AdminUser", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_AdminUser_CompanyId", table: "AdminUser", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_AdminUser_Email", table: "AdminUser", column: "Email", unique: true);

            // ── 3. LearningSession ───────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "LearningSession",
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
                    TrainingLinkId = table.Column<string>(type: "text", nullable: false),
                    LearnerKey = table.Column<string>(type: "text", nullable: false),
                    RecipientName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSlideObjectId = table.Column<string>(type: "text", nullable: true),
                    LastSlideIndex = table.Column<int>(type: "integer", nullable: false),
                    CompletedAllSlides = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_LearningSession", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_LearningSession_CompanyId", table: "LearningSession", column: "CompanyId");
            migrationBuilder.CreateIndex(
                name: "IX_LearningSession_TrainingLinkId_LearnerKey",
                table: "LearningSession",
                columns: ["TrainingLinkId", "LearnerKey"]);

            // ── 4. Split each old row's learning half out into its own LearningSession ────────
            // Only rows that someone actually opened. A link nobody ever used has no learning to
            // move, and inventing one would fabricate a learner who never existed. The EXISTS
            // clauses catch any row with children but no StartedAt (already-inconsistent data),
            // so nothing is left orphaned by step 5.
            //
            // LearnerKey gets a deterministic placeholder: the real key only ever lived in a
            // browser's local storage, and these sessions predate that mechanism entirely, so
            // nobody can resume them either way. It must still be non-null and distinct per row.
            migrationBuilder.Sql($"""
                INSERT INTO "LearningSession" (
                    "Id", "CompanyId", "CreateBy", "CreateDate", "UpdateBy", "UpdateDate",
                    "DeleteBy", "IsDelete", "DeletedAt",
                    "TrainingLinkId", "LearnerKey", "RecipientName", "Status",
                    "StartedAt", "EndedAt", "LastActivityAt",
                    "LastSlideObjectId", "LastSlideIndex", "CompletedAllSlides")
                SELECT
                    '{LegacyLearningPrefix}' || ts."Id",
                    ts."CompanyId", ts."CreateBy", ts."CreateDate", ts."UpdateBy", ts."UpdateDate",
                    ts."DeleteBy", ts."IsDelete", ts."DeletedAt",
                    ts."Id",
                    'legacy-' || ts."Id",
                    COALESCE(NULLIF(ts."RecipientName", ''), 'ผู้เรียน'),
                    CASE WHEN ts."Status" = 'ENDED' THEN 'ENDED' ELSE 'IN_PROGRESS' END,
                    COALESCE(ts."StartedAt", ts."CreateDate"),
                    ts."EndedAt",
                    COALESCE(ts."EndedAt", ts."StartedAt", ts."CreateDate"),
                    ts."LastSlideObjectId",
                    0,
                    ts."CompletedAllSlides"
                FROM "TrainingSession" ts
                WHERE ts."StartedAt" IS NOT NULL
                   OR EXISTS (SELECT 1 FROM "SessionQuestion" q WHERE q."SessionId" = ts."Id")
                   OR EXISTS (SELECT 1 FROM "ChatMessage" m WHERE m."SessionId" = ts."Id");
                """);

            // ── 5. Repoint the children at the learning session, not the link ────────────────
            // This is the whole point of the rename: SessionQuestion.SessionId and
            // ChatMessage.SessionId keep their names and finally mean what they say.
            migrationBuilder.Sql($"""
                UPDATE "SessionQuestion" q
                SET "SessionId" = '{LegacyLearningPrefix}' || q."SessionId"
                WHERE EXISTS (
                    SELECT 1 FROM "LearningSession" ls
                    WHERE ls."Id" = '{LegacyLearningPrefix}' || q."SessionId");
                """);

            migrationBuilder.Sql($"""
                UPDATE "ChatMessage" m
                SET "SessionId" = '{LegacyLearningPrefix}' || m."SessionId"
                WHERE EXISTS (
                    SELECT 1 FROM "LearningSession" ls
                    WHERE ls."Id" = '{LegacyLearningPrefix}' || m."SessionId");
                """);

            // ── 6. TrainingSession becomes TrainingLink, keeping its rows ────────────────────
            migrationBuilder.DropIndex(name: "IX_TrainingSession_Token", table: "TrainingSession");
            migrationBuilder.DropIndex(name: "IX_TrainingSession_CompanyId", table: "TrainingSession");

            migrationBuilder.RenameTable(name: "TrainingSession", newName: "TrainingLink");

            // Postgres does NOT rename a table's constraints along with the table, so the primary
            // key would stay called PK_TrainingSession on a table now named TrainingLink. Nothing
            // breaks today - a constraint's name has no effect on behaviour - but EF's model
            // snapshot records it as PK_TrainingLink, so the two silently disagree and the next
            // migration that touches the key looks for a name the database does not have.
            migrationBuilder.Sql("""
                ALTER TABLE "TrainingLink" RENAME CONSTRAINT "PK_TrainingSession" TO "PK_TrainingLink";
                """);

            // The columns that moved to LearningSession in step 4.
            migrationBuilder.DropColumn(name: "RecipientName", table: "TrainingLink");
            migrationBuilder.DropColumn(name: "Status", table: "TrainingLink");
            migrationBuilder.DropColumn(name: "StartedAt", table: "TrainingLink");
            migrationBuilder.DropColumn(name: "EndedAt", table: "TrainingLink");
            migrationBuilder.DropColumn(name: "CompletedAllSlides", table: "TrainingLink");
            migrationBuilder.DropColumn(name: "LastSlideObjectId", table: "TrainingLink");

            migrationBuilder.AddColumn<int>(name: "MaxAttendees", table: "TrainingLink", type: "integer", nullable: true);

            migrationBuilder.CreateIndex(name: "IX_TrainingLink_Token", table: "TrainingLink", column: "Token", unique: true);
            migrationBuilder.CreateIndex(name: "IX_TrainingLink_CompanyId", table: "TrainingLink", column: "CompanyId");

            // ── 7. CS's review verdict on each answer ────────────────────────────────────────
            migrationBuilder.AddColumn<string>(name: "ReviewResult", table: "SessionQuestion", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ReviewNote", table: "SessionQuestion", type: "text", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "ReviewedAt", table: "SessionQuestion", type: "timestamp with time zone", nullable: true);

            // ── 8. SessionSummary is gone ────────────────────────────────────────────────────
            // Two of its three columns now live on LearningSession and were carried across in
            // step 4; UnansweredPoints was always derivable from SessionQuestion and is computed
            // at read time instead (TD-013).
            migrationBuilder.DropTable(name: "SessionSummary");
        }

        /// <summary>
        /// Reverses the schema. Data cannot be fully restored: SessionSummary rows are gone for
        /// good (their surviving fields live on LearningSession, their derived field never needed
        /// storing), and a link opened by several people collapses back into the single row the
        /// old shape allowed - the earliest learner wins. Down() exists to unblock local
        /// experimentation, not as a production rollback plan.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionSummary",
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
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    CompletedAllSlides = table.Column<bool>(type: "boolean", nullable: false),
                    LastSlideObjectId = table.Column<string>(type: "text", nullable: true),
                    UnansweredPoints = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_SessionSummary", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_SessionSummary_CompanyId", table: "SessionSummary", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_SessionSummary_SessionId", table: "SessionSummary", column: "SessionId", unique: true);

            migrationBuilder.DropColumn(name: "ReviewResult", table: "SessionQuestion");
            migrationBuilder.DropColumn(name: "ReviewNote", table: "SessionQuestion");
            migrationBuilder.DropColumn(name: "ReviewedAt", table: "SessionQuestion");

            // Point the children back at the link before the learning sessions disappear.
            migrationBuilder.Sql($"""
                UPDATE "SessionQuestion" q
                SET "SessionId" = ls."TrainingLinkId"
                FROM "LearningSession" ls
                WHERE ls."Id" = q."SessionId";
                """);

            migrationBuilder.Sql($"""
                UPDATE "ChatMessage" m
                SET "SessionId" = ls."TrainingLinkId"
                FROM "LearningSession" ls
                WHERE ls."Id" = m."SessionId";
                """);

            migrationBuilder.DropIndex(name: "IX_TrainingLink_Token", table: "TrainingLink");
            migrationBuilder.DropIndex(name: "IX_TrainingLink_CompanyId", table: "TrainingLink");

            migrationBuilder.DropColumn(name: "MaxAttendees", table: "TrainingLink");

            migrationBuilder.AddColumn<string>(name: "RecipientName", table: "TrainingLink", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Status", table: "TrainingLink", type: "text", nullable: false, defaultValue: "NOT_STARTED");
            migrationBuilder.AddColumn<DateTime>(name: "StartedAt", table: "TrainingLink", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "EndedAt", table: "TrainingLink", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "CompletedAllSlides", table: "TrainingLink", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "LastSlideObjectId", table: "TrainingLink", type: "text", nullable: true);

            migrationBuilder.RenameTable(name: "TrainingLink", newName: "TrainingSession");

            migrationBuilder.Sql("""
                ALTER TABLE "TrainingSession" RENAME CONSTRAINT "PK_TrainingLink" TO "PK_TrainingSession";
                """);

            migrationBuilder.CreateIndex(name: "IX_TrainingSession_Token", table: "TrainingSession", column: "Token", unique: true);
            migrationBuilder.CreateIndex(name: "IX_TrainingSession_CompanyId", table: "TrainingSession", column: "CompanyId");

            // Fold the earliest learning session back onto its link - the old shape has room for
            // exactly one, so any others are dropped with the table below.
            migrationBuilder.Sql("""
                UPDATE "TrainingSession" ts
                SET "RecipientName" = first."RecipientName",
                    "Status" = first."Status",
                    "StartedAt" = first."StartedAt",
                    "EndedAt" = first."EndedAt",
                    "CompletedAllSlides" = first."CompletedAllSlides",
                    "LastSlideObjectId" = first."LastSlideObjectId"
                FROM (
                    SELECT DISTINCT ON ("TrainingLinkId")
                        "TrainingLinkId", "RecipientName", "Status", "StartedAt", "EndedAt",
                        "CompletedAllSlides", "LastSlideObjectId"
                    FROM "LearningSession"
                    ORDER BY "TrainingLinkId", "StartedAt"
                ) AS first
                WHERE first."TrainingLinkId" = ts."Id";
                """);

            migrationBuilder.DropTable(name: "LearningSession");
            migrationBuilder.DropTable(name: "AdminUser");
            migrationBuilder.DropTable(name: "Company");
        }
    }
}
