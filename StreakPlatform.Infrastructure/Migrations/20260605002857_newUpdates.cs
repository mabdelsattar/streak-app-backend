using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreakPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Participants_StreakId",
                table: "Participants");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Streaks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "InactiveAt",
                table: "Participants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InactiveReason",
                table: "Participants",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Participants",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "PointsPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PointsAdded = table.Column<int>(type: "int", nullable: false),
                    AmountUsdCents = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalReceiptId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsPurchases_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Streaks_IsPublic_CreatedAt",
                table: "Streaks",
                columns: new[] { "IsPublic", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Participants_StreakId_IsActive",
                table: "Participants",
                columns: new[] { "StreakId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsPurchases_ExternalReceiptId",
                table: "PointsPurchases",
                column: "ExternalReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsPurchases_UserId_CreatedAt",
                table: "PointsPurchases",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsPurchases");

            migrationBuilder.DropIndex(
                name: "IX_Streaks_IsPublic_CreatedAt",
                table: "Streaks");

            migrationBuilder.DropIndex(
                name: "IX_Participants_StreakId_IsActive",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Streaks");

            migrationBuilder.DropColumn(
                name: "InactiveAt",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "InactiveReason",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Participants");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_StreakId",
                table: "Participants",
                column: "StreakId");
        }
    }
}
