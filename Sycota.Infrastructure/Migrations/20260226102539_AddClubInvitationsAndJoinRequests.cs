using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sycota.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClubInvitationsAndJoinRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "Clubs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ClubInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OfferedRole = table.Column<int>(type: "int", nullable: false),
                    AssignedTrainerId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InvitationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubInvitations_AspNetUsers_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClubInvitations_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClubInvitations_ClubMembers_AssignedTrainerId",
                        column: x => x.AssignedTrainerId,
                        principalTable: "ClubMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClubInvitations_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubJoinRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    RequestedRole = table.Column<int>(type: "int", nullable: false),
                    RequestedTrainerId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubJoinRequests_AspNetUsers_ProcessedById",
                        column: x => x.ProcessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClubJoinRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClubJoinRequests_ClubMembers_RequestedTrainerId",
                        column: x => x.RequestedTrainerId,
                        principalTable: "ClubMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClubJoinRequests_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubInvitations_AcceptedByUserId",
                table: "ClubInvitations",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubInvitations_AssignedTrainerId",
                table: "ClubInvitations",
                column: "AssignedTrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubInvitations_ClubId_Email_Status",
                table: "ClubInvitations",
                columns: new[] { "ClubId", "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClubInvitations_CreatedById",
                table: "ClubInvitations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClubInvitations_InvitationCode",
                table: "ClubInvitations",
                column: "InvitationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubJoinRequests_ClubId",
                table: "ClubJoinRequests",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubJoinRequests_ProcessedById",
                table: "ClubJoinRequests",
                column: "ProcessedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClubJoinRequests_RequestedTrainerId",
                table: "ClubJoinRequests",
                column: "RequestedTrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubJoinRequests_UserId_ClubId_Status",
                table: "ClubJoinRequests",
                columns: new[] { "UserId", "ClubId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubInvitations");

            migrationBuilder.DropTable(
                name: "ClubJoinRequests");

            migrationBuilder.DropColumn(
                name: "RequiresApproval",
                table: "Clubs");
        }
    }
}
