using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeTaxonomyAndScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MG-A1

            migrationBuilder.CreateTable(
                name: "KnowledgeCategory",
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
                    ParentId = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeCategory", x => x.Id);
                });

            migrationBuilder.AddColumn<string>(name: "CategoryId", table: "LessonConfig", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ScopeType", table: "DocumentResource", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ScopeId", table: "DocumentResource", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FailureReason", table: "DocumentResource", type: "text", nullable: true);
            migrationBuilder.Sql("""
                INSERT INTO "KnowledgeCategory" ("Id", "CompanyId", "CreateDate", "IsDelete", "ParentId", "Level", "Name", "SortOrder", "IsSystemDefault")
                SELECT 'kbcat-backfill-parent-' || md5("CompanyId"), "CompanyId", now(), false, null, 1, 'ยังไม่จัดหมวด', 9999, true FROM (SELECT "CompanyId" FROM "LessonConfig" UNION SELECT "CompanyId" FROM "DocumentResource") companies ON CONFLICT ("Id") DO NOTHING;
                INSERT INTO "KnowledgeCategory" ("Id", "CompanyId", "CreateDate", "IsDelete", "ParentId", "Level", "Name", "SortOrder", "IsSystemDefault")
                SELECT 'kbcat-backfill-child-' || md5("CompanyId"), "CompanyId", now(), false, 'kbcat-backfill-parent-' || md5("CompanyId"), 2, 'ยังไม่จัดหมวด', 9999, true FROM (SELECT "CompanyId" FROM "LessonConfig" UNION SELECT "CompanyId" FROM "DocumentResource") companies ON CONFLICT ("Id") DO NOTHING;
                UPDATE "LessonConfig" SET "CategoryId" = 'kbcat-backfill-child-' || md5("CompanyId") WHERE "CategoryId" IS NULL;
                UPDATE "DocumentResource" SET "ScopeType" = CASE WHEN "LessonId" IS NULL THEN 'company' ELSE 'lesson' END, "ScopeId" = "LessonId" WHERE "ScopeType" IS NULL;
                """);
            migrationBuilder.AlterColumn<string>(name: "CategoryId", table: "LessonConfig", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "ScopeType", table: "DocumentResource", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.DropIndex(name: "IX_DocumentResource_CompanyId", table: "DocumentResource");
            migrationBuilder.DropIndex(name: "IX_DocumentResource_LessonId", table: "DocumentResource");
            migrationBuilder.DropColumn(name: "LessonId", table: "DocumentResource");

            migrationBuilder.CreateIndex(
                name: "IX_LessonConfig_CategoryId",
                table: "LessonConfig",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentResource_CompanyId_ScopeType_ScopeId",
                table: "DocumentResource",
                columns: new[] { "CompanyId", "ScopeType", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeCategory_CompanyId",
                table: "KnowledgeCategory",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeCategory_CompanyId_ParentId_SortOrder",
                table: "KnowledgeCategory",
                columns: new[] { "CompanyId", "ParentId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // MG-A7 is intentionally lossy: category-scoped rows become legacy standalone rows.
            migrationBuilder.AddColumn<string>(name: "LessonId", table: "DocumentResource", type: "text", nullable: true);
            migrationBuilder.Sql("""UPDATE "DocumentResource" SET "LessonId" = "ScopeId" WHERE "ScopeType" = 'lesson';""");

            migrationBuilder.DropIndex(
                name: "IX_LessonConfig_CategoryId",
                table: "LessonConfig");

            migrationBuilder.DropIndex(
                name: "IX_DocumentResource_CompanyId_ScopeType_ScopeId",
                table: "DocumentResource");

            migrationBuilder.DropColumn(name: "CategoryId", table: "LessonConfig");

            migrationBuilder.DropColumn(name: "FailureReason", table: "DocumentResource");
            migrationBuilder.DropColumn(name: "ScopeType", table: "DocumentResource");
            migrationBuilder.DropColumn(name: "ScopeId", table: "DocumentResource");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentResource_CompanyId",
                table: "DocumentResource",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentResource_LessonId",
                table: "DocumentResource",
                column: "LessonId");

            migrationBuilder.DropTable(name: "KnowledgeCategory");
        }
    }
}
