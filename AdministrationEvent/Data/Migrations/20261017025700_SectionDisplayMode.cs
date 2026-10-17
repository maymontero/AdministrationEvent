using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdministrationEvent.Data.Migrations
{
    /// <inheritdoc />
    public partial class SectionDisplayMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayMode",
                table: "InvitationSections",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "standard");

            // Preserve the intended presentation of existing beach sections.
            migrationBuilder.Sql("UPDATE InvitationSections SET DisplayMode = 'art-above' WHERE ImageUrl IN ('/images/despedida/calendario-playita-v2.png', '/images/despedida/avioncito-detalle-v1.png')");
            migrationBuilder.Sql("UPDATE InvitationSections SET DisplayMode = 'backdrop' WHERE ImageUrl = '/images/despedida/plan-cinco-sillitas-dibujo-v2.png'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayMode",
                table: "InvitationSections");
        }
    }
}
