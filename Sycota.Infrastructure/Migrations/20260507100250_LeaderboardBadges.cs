using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sycota.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LeaderboardBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty.
            // BadgeNotifications is created in 20260507090322_AddBadgeNotificationsPersistence.
            // This migration remains as a no-op to avoid duplicate CREATE TABLE execution.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty.
        }
    }
}
