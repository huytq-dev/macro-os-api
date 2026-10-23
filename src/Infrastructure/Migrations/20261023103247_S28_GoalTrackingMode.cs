using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S28_GoalTrackingMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StartScaleWeightKg",
                table: "Goals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingMode",
                table: "Goals",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Trend");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Goals_TrackingMode",
                table: "Goals",
                sql: "[TrackingMode] IN (N'Trend', N'Scale')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Goals_TrackingMode",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "StartScaleWeightKg",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "TrackingMode",
                table: "Goals");
        }
    }
}
