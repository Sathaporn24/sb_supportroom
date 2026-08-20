using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportRoom.Providers.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableIndexingJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundJob",
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
                    JobType = table.Column<string>(type: "text", nullable: false),
                    TargetId = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "text", nullable: true),
                    LastErrorDetail = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJob", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJob_CompanyId_JobType_TargetId",
                table: "BackgroundJob",
                columns: new[] { "CompanyId", "JobType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJob_Status_NextAttemptAt",
                table: "BackgroundJob",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJob");
        }
    }
}
