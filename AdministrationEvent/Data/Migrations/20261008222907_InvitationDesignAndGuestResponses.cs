using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace AdministrationEvent.Data.Migrations
{
    /// <inheritdoc />
    public partial class InvitationDesignAndGuestResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Layout",
                table: "InvitationThemes",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "classic");

            migrationBuilder.AddColumn<bool>(
                name: "HasConfirmedTime",
                table: "Invitations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "RsvpEnabled",
                table: "Invitations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCountdown",
                table: "Invitations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Invitations",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "America/Montevideo");

            migrationBuilder.CreateTable(
                name: "GuestResponses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Contact = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Attending = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestResponses_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GuestResponses_InvitationId_SubmittedAtUtc",
                table: "GuestResponses",
                columns: new[] { "InvitationId", "SubmittedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestResponses");

            migrationBuilder.DropColumn(
                name: "Layout",
                table: "InvitationThemes");

            migrationBuilder.DropColumn(
                name: "HasConfirmedTime",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "RsvpEnabled",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "ShowCountdown",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Invitations");
        }
    }
}
