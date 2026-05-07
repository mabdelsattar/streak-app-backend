using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreakPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresProof",
                table: "Streaks");

            migrationBuilder.AddColumn<string>(
                name: "CheckInButtonLabel",
                table: "Streaks",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckInType",
                table: "Streaks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MediaDurationSeconds",
                table: "CheckIns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CheckInReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckInId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReactorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckInReactions_CheckIns_CheckInId",
                        column: x => x.CheckInId,
                        principalTable: "CheckIns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckInReactions_Users_ReactorUserId",
                        column: x => x.ReactorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInReactions_CheckInId",
                table: "CheckInReactions",
                column: "CheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInReactions_CheckInId_ReactorUserId",
                table: "CheckInReactions",
                columns: new[] { "CheckInId", "ReactorUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckInReactions_ReactorUserId",
                table: "CheckInReactions",
                column: "ReactorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckInReactions");

            migrationBuilder.DropColumn(
                name: "CheckInButtonLabel",
                table: "Streaks");

            migrationBuilder.DropColumn(
                name: "CheckInType",
                table: "Streaks");

            migrationBuilder.DropColumn(
                name: "MediaDurationSeconds",
                table: "CheckIns");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresProof",
                table: "Streaks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
