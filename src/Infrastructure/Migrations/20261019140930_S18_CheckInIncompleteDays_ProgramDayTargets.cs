using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S18_CheckInIncompleteDays_ProgramDayTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CheckInIntroSeen",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PartialLoggingModuleHidden",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "MacroProgramDayTargets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsIncomplete",
                table: "FoodLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInIntroSeen",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "PartialLoggingModuleHidden",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "MacroProgramDayTargets");

            migrationBuilder.DropColumn(
                name: "IsIncomplete",
                table: "FoodLogs");
        }
    }
}
