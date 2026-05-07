using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sycota.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixesToCascading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubWeapons_ClubMembers_AssignedShooterId",
                table: "ClubWeapons");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryIssues_ClubAmmo_AmmoId",
                table: "InventoryIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryIssues_ClubWeapons_WeaponId",
                table: "InventoryIssues");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubWeapons_ClubMembers_AssignedShooterId",
                table: "ClubWeapons",
                column: "AssignedShooterId",
                principalTable: "ClubMembers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryIssues_ClubAmmo_AmmoId",
                table: "InventoryIssues",
                column: "AmmoId",
                principalTable: "ClubAmmo",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryIssues_ClubWeapons_WeaponId",
                table: "InventoryIssues",
                column: "WeaponId",
                principalTable: "ClubWeapons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubWeapons_ClubMembers_AssignedShooterId",
                table: "ClubWeapons");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryIssues_ClubAmmo_AmmoId",
                table: "InventoryIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryIssues_ClubWeapons_WeaponId",
                table: "InventoryIssues");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubWeapons_ClubMembers_AssignedShooterId",
                table: "ClubWeapons",
                column: "AssignedShooterId",
                principalTable: "ClubMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryIssues_ClubAmmo_AmmoId",
                table: "InventoryIssues",
                column: "AmmoId",
                principalTable: "ClubAmmo",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryIssues_ClubWeapons_WeaponId",
                table: "InventoryIssues",
                column: "WeaponId",
                principalTable: "ClubWeapons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
