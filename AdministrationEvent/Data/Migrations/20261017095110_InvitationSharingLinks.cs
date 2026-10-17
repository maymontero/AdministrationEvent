using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdministrationEvent.Data.Migrations
{
    /// <inheritdoc />
    public partial class InvitationSharingLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShareKey",
                table: "Invitations",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharingMode",
                table: "Invitations",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "public");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ShareKey",
                table: "Invitations",
                column: "ShareKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invitations_ShareKey",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "ShareKey",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "SharingMode",
                table: "Invitations");
        }
    }
}
