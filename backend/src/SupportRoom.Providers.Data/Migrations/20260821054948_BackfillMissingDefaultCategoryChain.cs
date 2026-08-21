using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMissingDefaultCategoryChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "KnowledgeCategory" (
                    "Id", "CompanyId", "CreateBy", "CreateDate", "UpdateBy", "UpdateDate",
                    "DeleteBy", "IsDelete", "DeletedAt", "ParentId", "Level", "Name",
                    "Description", "SortOrder", "IsSystemDefault")
                SELECT
                    'kbcat-company-admin-parent-' || md5(company."Id"),
                    company."Id",
                    NULL,
                    now(),
                    NULL,
                    NULL,
                    NULL,
                    false,
                    NULL,
                    NULL,
                    1,
                    'ยังไม่จัดหมวด',
                    NULL,
                    9999,
                    true
                FROM "Company" company
                WHERE (
                    SELECT COUNT(*)
                    FROM "KnowledgeCategory" leaf
                    WHERE leaf."CompanyId" = company."Id"
                      AND leaf."IsSystemDefault" = true
                      AND leaf."Level" = 2
                ) <= 1
                  AND NOT EXISTS (
                    SELECT 1
                    FROM "KnowledgeCategory" parent
                    WHERE parent."CompanyId" = company."Id"
                      AND parent."IsSystemDefault" = true
                      AND parent."Level" = 1
                );

                UPDATE "KnowledgeCategory" leaf
                SET "ParentId" = 'kbcat-company-admin-parent-' || md5(leaf."CompanyId")
                WHERE leaf."IsSystemDefault" = true
                  AND leaf."Level" = 2
                  AND (
                    SELECT COUNT(*)
                    FROM "KnowledgeCategory" sibling
                    WHERE sibling."CompanyId" = leaf."CompanyId"
                      AND sibling."IsSystemDefault" = true
                      AND sibling."Level" = 2
                  ) = 1
                  AND EXISTS (
                    SELECT 1
                    FROM "KnowledgeCategory" parent
                    WHERE parent."Id" = 'kbcat-company-admin-parent-' || md5(leaf."CompanyId")
                      AND parent."CompanyId" = leaf."CompanyId"
                      AND parent."IsSystemDefault" = true
                      AND parent."Level" = 1
                );

                INSERT INTO "KnowledgeCategory" (
                    "Id", "CompanyId", "CreateBy", "CreateDate", "UpdateBy", "UpdateDate",
                    "DeleteBy", "IsDelete", "DeletedAt", "ParentId", "Level", "Name",
                    "Description", "SortOrder", "IsSystemDefault")
                SELECT
                    'kbcat-company-admin-leaf-' || md5(company."Id"),
                    company."Id",
                    NULL,
                    parent."CreateDate",
                    NULL,
                    NULL,
                    NULL,
                    false,
                    NULL,
                    parent."Id",
                    2,
                    'ยังไม่จัดหมวด',
                    NULL,
                    9999,
                    true
                FROM "Company" company
                JOIN LATERAL (
                    SELECT candidate."Id", candidate."CreateDate"
                    FROM "KnowledgeCategory" candidate
                    WHERE candidate."CompanyId" = company."Id"
                      AND candidate."IsSystemDefault" = true
                      AND candidate."Level" = 1
                    ORDER BY candidate."Id"
                    LIMIT 1
                ) parent ON true
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "KnowledgeCategory" leaf
                    WHERE leaf."CompanyId" = company."Id"
                      AND leaf."IsSystemDefault" = true
                      AND leaf."Level" = 2
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: existing default rows and rows created here have the same business shape,
            // so deleting by shape could destroy a chain that was already in active use.
        }
    }
}
