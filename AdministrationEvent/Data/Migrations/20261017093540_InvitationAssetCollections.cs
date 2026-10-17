using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdministrationEvent.Data.Migrations
{
    /// <inheritdoc />
    public partial class InvitationAssetCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetCollection",
                table: "Invitations",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "shared");

            migrationBuilder.Sql("UPDATE Invitations i LEFT JOIN InvitationSections s ON s.InvitationId = i.Id SET i.AssetCollection = 'despedida' WHERE i.CoverImageUrl LIKE '/images/despedida/%' OR s.ImageUrl LIKE '/images/despedida/%'");
            migrationBuilder.Sql("UPDATE Invitations i LEFT JOIN InvitationSections s ON s.InvitationId = i.Id SET i.AssetCollection = 'zenia-diego' WHERE i.CoverImageUrl LIKE '/images/zenia-diego/%' OR s.ImageUrl LIKE '/images/zenia-diego/%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetCollection",
                table: "Invitations");
        }
    }
}
