using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreakPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPointingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointsBalance",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresProof",
                table: "Streaks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MediaContentType",
                table: "CheckIns",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "CheckIns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PointsTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Delta = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    RelatedStreakId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedProtectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StreakProtections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StreakId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PointsCost = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreakProtections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreakProtections_Streaks_StreakId",
                        column: x => x.StreakId,
                        principalTable: "Streaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StreakProtections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_StreakId_CreatedAt",
                table: "CheckIns",
                columns: new[] { "StreakId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsTransactions_UserId_CreatedAt",
                table: "PointsTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StreakProtections_OnePendingPerUserStreak",
                table: "StreakProtections",
                columns: new[] { "UserId", "StreakId", "Status" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StreakProtections_StreakId",
                table: "StreakProtections",
                column: "StreakId");

            migrationBuilder.CreateIndex(
                name: "IX_StreakProtections_UserId_StreakId_AppliedToDate",
                table: "StreakProtections",
                columns: new[] { "UserId", "StreakId", "AppliedToDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsTransactions");

            migrationBuilder.DropTable(
                name: "StreakProtections");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_StreakId_CreatedAt",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "PointsBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequiresProof",
                table: "Streaks");

            migrationBuilder.DropColumn(
                name: "MediaContentType",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "CheckIns");
        }
    }
}
