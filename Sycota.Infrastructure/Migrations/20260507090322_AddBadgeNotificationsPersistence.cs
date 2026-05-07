using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sycota.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeNotificationsPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BadgeNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    ClubName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BadgeTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BadgeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnlockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadgeNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeNotifications_UserId_ClubId_BadgeTitle",
                table: "BadgeNotifications",
                columns: new[] { "UserId", "ClubId", "BadgeTitle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BadgeNotifications_UserId_IsRead_UnlockedAtUtc",
                table: "BadgeNotifications",
                columns: new[] { "UserId", "IsRead", "UnlockedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadgeNotifications");
        }
    }
}
