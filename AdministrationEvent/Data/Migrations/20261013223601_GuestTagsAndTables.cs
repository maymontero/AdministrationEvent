using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace AdministrationEvent.Data.Migrations
{
    /// <inheritdoc />
    public partial class GuestTagsAndTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TableId",
                table: "Guests",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuestTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestTags_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SeatingTables",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatingTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatingTables_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GuestTagAssignments",
                columns: table => new
                {
                    GuestId = table.Column<long>(type: "bigint", nullable: false),
                    TagId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestTagAssignments", x => new { x.GuestId, x.TagId });
                    table.ForeignKey(
                        name: "FK_GuestTagAssignments_GuestTags_TagId",
                        column: x => x.TagId,
                        principalTable: "GuestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuestTagAssignments_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_TableId",
                table: "Guests",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestTagAssignments_TagId",
                table: "GuestTagAssignments",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestTags_InvitationId_Name",
                table: "GuestTags",
                columns: new[] { "InvitationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatingTables_InvitationId_Name",
                table: "SeatingTables",
                columns: new[] { "InvitationId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Guests_SeatingTables_TableId",
                table: "Guests",
                column: "TableId",
                principalTable: "SeatingTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guests_SeatingTables_TableId",
                table: "Guests");

            migrationBuilder.DropTable(
                name: "GuestTagAssignments");

            migrationBuilder.DropTable(
                name: "SeatingTables");

            migrationBuilder.DropTable(
                name: "GuestTags");

            migrationBuilder.DropIndex(
                name: "IX_Guests_TableId",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "TableId",
                table: "Guests");
        }
    }
}
