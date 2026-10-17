using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdministrationEvent.Data.Migrations
{
    /// <inheritdoc />
    public partial class InvitationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvitationAccesses",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    InvitationId = table.Column<long>(type: "bigint", nullable: false),
                    AvailableFromUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationAccesses", x => new { x.UserId, x.InvitationId });
                    table.ForeignKey(
                        name: "FK_InvitationAccesses_AdminUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvitationAccesses_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationAccesses_InvitationId",
                table: "InvitationAccesses",
                column: "InvitationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvitationAccesses");
        }
    }
}
