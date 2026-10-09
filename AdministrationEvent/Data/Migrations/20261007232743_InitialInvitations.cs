using System;
using MySql.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AdministrationEvent.Migrations
{
    public partial class InitialInvitations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PublicId = table.Column<Guid>(nullable: false),
                    EventType = table.Column<string>(maxLength: 80, nullable: false),
                    Hosts = table.Column<string>(maxLength: 200, nullable: true),
                    Date = table.Column<DateTime>(nullable: true),
                    Message = table.Column<string>(maxLength: 2000, nullable: true),
                    CoverImageUrl = table.Column<string>(maxLength: 2000, nullable: true),
                    Title = table.Column<string>(maxLength: 250, nullable: false),
                    Slug = table.Column<string>(maxLength: 250, nullable: false),
                    IsPublished = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvitationSections",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationId = table.Column<long>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    Enabled = table.Column<bool>(nullable: false),
                    Title = table.Column<string>(maxLength: 120, nullable: false),
                    Content = table.Column<string>(type: "longtext", maxLength: 5000, nullable: true),
                    ImageUrl = table.Column<string>(maxLength: 2000, nullable: true),
                    LinkLabel = table.Column<string>(maxLength: 80, nullable: true),
                    LinkUrl = table.Column<string>(maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitationSections_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvitationThemes",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CornerRadius = table.Column<int>(nullable: false),
                    FontSize = table.Column<int>(nullable: false),
                    ContentWidth = table.Column<int>(nullable: false),
                    InvitationId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationThemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitationThemes_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RSVPConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationId = table.Column<long>(nullable: false),
                    Enabled = table.Column<bool>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    GoogleFormUrl = table.Column<string>(nullable: true),
                    ConfirmationDeadline = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RSVPConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RSVPConfigurations_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationId = table.Column<long>(nullable: false),
                    Title = table.Column<string>(maxLength: 250, nullable: true),
                    DateTime = table.Column<DateTime>(nullable: true),
                    DressCode = table.Column<string>(maxLength: 500, nullable: true),
                    Order = table.Column<int>(nullable: false),
                    LocationId = table.Column<long>(nullable: true),
                    ParentEventId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Events_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Events_Events_ParentEventId",
                        column: x => x.ParentEventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AudioResources",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationThemeId = table.Column<long>(nullable: false),
                    Key = table.Column<string>(maxLength: 100, nullable: false),
                    Url = table.Column<string>(maxLength: 2000, nullable: false),
                    Loop = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioResources_InvitationThemes_InvitationThemeId",
                        column: x => x.InvitationThemeId,
                        principalTable: "InvitationThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ColorResources",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationThemeId = table.Column<long>(nullable: false),
                    Key = table.Column<string>(maxLength: 100, nullable: false),
                    Value = table.Column<string>(maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColorResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColorResources_InvitationThemes_InvitationThemeId",
                        column: x => x.InvitationThemeId,
                        principalTable: "InvitationThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FontResources",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationThemeId = table.Column<long>(nullable: false),
                    Key = table.Column<string>(maxLength: 100, nullable: false),
                    FontFamily = table.Column<string>(maxLength: 250, nullable: false),
                    Url = table.Column<string>(maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FontResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FontResources_InvitationThemes_InvitationThemeId",
                        column: x => x.InvitationThemeId,
                        principalTable: "InvitationThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageResources",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    InvitationThemeId = table.Column<long>(nullable: false),
                    Key = table.Column<string>(maxLength: 100, nullable: false),
                    Url = table.Column<string>(maxLength: 2000, nullable: false),
                    Alt = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageResources_InvitationThemes_InvitationThemeId",
                        column: x => x.InvitationThemeId,
                        principalTable: "InvitationThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioResources_InvitationThemeId_Key",
                table: "AudioResources",
                columns: new[] { "InvitationThemeId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ColorResources_InvitationThemeId_Key",
                table: "ColorResources",
                columns: new[] { "InvitationThemeId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_LocationId",
                table: "Events",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ParentEventId",
                table: "Events",
                column: "ParentEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_InvitationId_Order",
                table: "Events",
                columns: new[] { "InvitationId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_FontResources_InvitationThemeId_Key",
                table: "FontResources",
                columns: new[] { "InvitationThemeId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageResources_InvitationThemeId_Key",
                table: "ImageResources",
                columns: new[] { "InvitationThemeId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_PublicId",
                table: "Invitations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Slug",
                table: "Invitations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvitationSections_InvitationId_Order",
                table: "InvitationSections",
                columns: new[] { "InvitationId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitationThemes_InvitationId",
                table: "InvitationThemes",
                column: "InvitationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RSVPConfigurations_InvitationId",
                table: "RSVPConfigurations",
                column: "InvitationId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioResources");

            migrationBuilder.DropTable(
                name: "ColorResources");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "FontResources");

            migrationBuilder.DropTable(
                name: "ImageResources");

            migrationBuilder.DropTable(
                name: "InvitationSections");

            migrationBuilder.DropTable(
                name: "RSVPConfigurations");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "InvitationThemes");

            migrationBuilder.DropTable(
                name: "Invitations");
        }
    }
}
