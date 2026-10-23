using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdministrationEvent.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptionalSeatingPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SeatingPlanEnabled",
                table: "Invitations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Preserve existing seating work; other invitations start without a floor plan.
            migrationBuilder.Sql(@"UPDATE `Invitations` i SET `SeatingPlanEnabled` = 1
                WHERE EXISTS (SELECT 1 FROM `SeatingPlans` p WHERE p.`InvitationId` = i.`Id`)
                   OR EXISTS (SELECT 1 FROM `SeatingTables` t WHERE t.`InvitationId` = i.`Id`)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeatingPlanEnabled",
                table: "Invitations");
        }
    }
}
