using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sycota.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClubInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClubAmmo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubAmmo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubAmmo_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubWeapons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssignedShooterId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubWeapons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubWeapons_ClubMembers_AssignedShooterId",
                        column: x => x.AssignedShooterId,
                        principalTable: "ClubMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClubWeapons_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    ShooterId = table.Column<int>(type: "int", nullable: false),
                    IssuedById = table.Column<int>(type: "int", nullable: false),
                    WeaponId = table.Column<int>(type: "int", nullable: true),
                    AmmoId = table.Column<int>(type: "int", nullable: true),
                    AmmoQuantity = table.Column<int>(type: "int", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryIssues_ClubAmmo_AmmoId",
                        column: x => x.AmmoId,
                        principalTable: "ClubAmmo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryIssues_ClubMembers_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "ClubMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryIssues_ClubMembers_ShooterId",
                        column: x => x.ShooterId,
                        principalTable: "ClubMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryIssues_ClubWeapons_WeaponId",
                        column: x => x.WeaponId,
                        principalTable: "ClubWeapons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryIssues_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubAmmo_ClubId_SerialNumber",
                table: "ClubAmmo",
                columns: new[] { "ClubId", "SerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubWeapons_AssignedShooterId",
                table: "ClubWeapons",
                column: "AssignedShooterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubWeapons_ClubId_SerialNumber",
                table: "ClubWeapons",
                columns: new[] { "ClubId", "SerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_AmmoId",
                table: "InventoryIssues",
                column: "AmmoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_ClubId_IssuedAt",
                table: "InventoryIssues",
                columns: new[] { "ClubId", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_IssuedById",
                table: "InventoryIssues",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_ShooterId",
                table: "InventoryIssues",
                column: "ShooterId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryIssues_WeaponId",
                table: "InventoryIssues",
                column: "WeaponId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryIssues");

            migrationBuilder.DropTable(
                name: "ClubAmmo");

            migrationBuilder.DropTable(
                name: "ClubWeapons");
        }
    }
}
