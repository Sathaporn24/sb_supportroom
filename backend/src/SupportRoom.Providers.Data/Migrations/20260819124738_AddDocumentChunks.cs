using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentChunk",
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
                    DocumentId = table.Column<string>(type: "text", nullable: false),
                    ChunkKey = table.Column<string>(type: "text", nullable: false),
                    VectorId = table.Column<string>(type: "text", nullable: false),
                    NamespaceKey = table.Column<string>(type: "text", nullable: false),
                    SeqNo = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CharCount = table.Column<int>(type: "integer", nullable: false),
                    HasSuspectCharacters = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunk", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunk_CompanyId",
                table: "DocumentChunk",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunk_DocumentId_SeqNo",
                table: "DocumentChunk",
                columns: new[] { "DocumentId", "SeqNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentChunk");
        }
    }
}
