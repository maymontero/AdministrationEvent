using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdministrationEvent.Data.Migrations
{
    /// <inheritdoc />
    public partial class DefaultCustomLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Layout",
                table: "InvitationThemes",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "custom",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "classic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Layout",
                table: "InvitationThemes",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "classic",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "custom");
        }
    }
}
