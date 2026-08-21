using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class CorrectDefaultCategoryChainLeafCreateDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "KnowledgeCategory" leaf
                SET "CreateDate" = now()
                FROM "KnowledgeCategory" parent
                WHERE leaf."Id" = 'kbcat-company-admin-leaf-' || md5(leaf."CompanyId")
                  AND leaf."CompanyId" = parent."CompanyId"
                  AND leaf."ParentId" = parent."Id"
                  AND leaf."IsSystemDefault" = true
                  AND leaf."Level" = 2
                  AND parent."IsSystemDefault" = true
                  AND parent."Level" = 1
                  AND parent."Id" <> 'kbcat-company-admin-parent-' || md5(parent."CompanyId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: the original creation timestamp cannot be recovered safely.
        }
    }
}
