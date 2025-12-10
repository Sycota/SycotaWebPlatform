using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sycota.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovedClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shots");

            migrationBuilder.DropTable(
                name: "TrainingScores");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TrainingSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Shots",
                table: "TrainingSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WeaponType",
                table: "TrainingSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "Shots",
                table: "TrainingSessions");

            migrationBuilder.DropColumn(
                name: "WeaponType",
                table: "TrainingSessions");

            migrationBuilder.CreateTable(
                name: "TrainingScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubMemberId = table.Column<int>(type: "int", nullable: false),
                    SubmittedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TrainingSessionId = table.Column<int>(type: "int", nullable: false),
                    AverageScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SeriesCount = table.Column<int>(type: "int", nullable: false),
                    SeriesScores = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShotsCount = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(18,1)", precision: 18, scale: 1, nullable: false),
                    WeaponType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingScores_AspNetUsers_SubmittedById",
                        column: x => x.SubmittedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingScores_ClubMembers_ClubMemberId",
                        column: x => x.ClubMemberId,
                        principalTable: "ClubMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingScores_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingScoreId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    SeriesNumber = table.Column<int>(type: "int", nullable: false),
                    ShotNumber = table.Column<int>(type: "int", nullable: false),
                    ShotOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shots_TrainingScores_TrainingScoreId",
                        column: x => x.TrainingScoreId,
                        principalTable: "TrainingScores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shots_TrainingScoreId_SeriesNumber_ShotNumber",
                table: "Shots",
                columns: new[] { "TrainingScoreId", "SeriesNumber", "ShotNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScores_ClubMemberId",
                table: "TrainingScores",
                column: "ClubMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScores_SubmittedById",
                table: "TrainingScores",
                column: "SubmittedById");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScores_TrainingSessionId",
                table: "TrainingScores",
                column: "TrainingSessionId");
        }
    }
}
