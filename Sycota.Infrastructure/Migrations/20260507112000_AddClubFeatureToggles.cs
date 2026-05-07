using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sycota.Infrastructure.Migrations
{
    public partial class AddClubFeatureToggles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGamificationEnabled",
                table: "Clubs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInventoryEnabled",
                table: "Clubs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLeaderboardEnabled",
                table: "Clubs",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGamificationEnabled",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "IsInventoryEnabled",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "IsLeaderboardEnabled",
                table: "Clubs");
        }
    }
}
