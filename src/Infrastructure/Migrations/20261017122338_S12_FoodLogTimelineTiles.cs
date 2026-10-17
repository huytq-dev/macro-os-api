using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S12_FoodLogTimelineTiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimelineHighlight",
                table: "FoodLogSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Edit"); // dòng đã lưu nhận mặc định; model luôn ghi tường minh (quy ước S8)

            migrationBuilder.AddColumn<bool>(
                name: "TimelineShowMacros",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TimelineShowServing",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "TimelineShowWeight",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimelineTileDensity",
                table: "FoodLogSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Standard");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimelineHighlight",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "TimelineShowMacros",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "TimelineShowServing",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "TimelineShowWeight",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "TimelineTileDensity",
                table: "FoodLogSettings");
        }
    }
}
