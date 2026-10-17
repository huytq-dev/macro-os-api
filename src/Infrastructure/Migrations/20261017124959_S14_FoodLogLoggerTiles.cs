using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S14_FoodLogLoggerTiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoggerHighlight",
                table: "FoodLogSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MultiAdd"); // dòng đã lưu nhận mặc định; model luôn ghi tường minh (quy ước S8)

            migrationBuilder.AddColumn<bool>(
                name: "LoggerShowMacros",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "LoggerShowServing",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "LoggerShowWeight",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LoggerTileDensity",
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
                name: "LoggerHighlight",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "LoggerShowMacros",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "LoggerShowServing",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "LoggerShowWeight",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "LoggerTileDensity",
                table: "FoodLogSettings");
        }
    }
}
