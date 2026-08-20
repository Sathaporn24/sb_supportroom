using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeQnA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeQnA",
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
                    Question = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    ScopeType = table.Column<string>(type: "text", nullable: false),
                    ScopeId = table.Column<string>(type: "text", nullable: true),
                    VectorId = table.Column<string>(type: "text", nullable: false),
                    IndexedNamespaceKey = table.Column<string>(type: "text", nullable: true),
                    IndexingStatus = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQnA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeQnAConflict",
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
                    QnAId = table.Column<string>(type: "text", nullable: false),
                    SessionQuestionId = table.Column<string>(type: "text", nullable: true),
                    ConflictingSourceLabel = table.Column<string>(type: "text", nullable: false),
                    ModelNote = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQnAConflict", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeQnASource",
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
                    QnAId = table.Column<string>(type: "text", nullable: false),
                    SessionQuestionId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeQnASource", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionQuestion_CompanyId_AnswerStatus",
                table: "SessionQuestion",
                columns: new[] { "CompanyId", "AnswerStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionQuestion_CompanyId_ReviewResult",
                table: "SessionQuestion",
                columns: new[] { "CompanyId", "ReviewResult" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQnA_CompanyId_ScopeType_ScopeId",
                table: "KnowledgeQnA",
                columns: new[] { "CompanyId", "ScopeType", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQnAConflict_CompanyId_ResolvedAt",
                table: "KnowledgeQnAConflict",
                columns: new[] { "CompanyId", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQnAConflict_QnAId",
                table: "KnowledgeQnAConflict",
                column: "QnAId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQnASource_CompanyId_SessionQuestionId",
                table: "KnowledgeQnASource",
                columns: new[] { "CompanyId", "SessionQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeQnASource_QnAId",
                table: "KnowledgeQnASource",
                column: "QnAId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeQnA");

            migrationBuilder.DropTable(
                name: "KnowledgeQnAConflict");

            migrationBuilder.DropTable(
                name: "KnowledgeQnASource");

            migrationBuilder.DropIndex(
                name: "IX_SessionQuestion_CompanyId_AnswerStatus",
                table: "SessionQuestion");

            migrationBuilder.DropIndex(
                name: "IX_SessionQuestion_CompanyId_ReviewResult",
                table: "SessionQuestion");
        }
    }
}
