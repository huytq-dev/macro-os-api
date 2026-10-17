using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S13_FoodLogLoggerBanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoggerBannerDisplay",
                table: "FoodLogSettings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PlateOnly"); // dòng đã lưu nhận mặc định; model luôn ghi tường minh (quy ước S8)

            migrationBuilder.AddColumn<bool>(
                name: "LoggerBannerShowCalories",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "LoggerBannerShowCarbs",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LoggerBannerShowFat",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LoggerBannerShowProtein",
                table: "FoodLogSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoggerBannerDisplay",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "LoggerBannerShowCalories",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "LoggerBannerShowCarbs",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "LoggerBannerShowFat",
                table: "FoodLogSettings");

            migrationBuilder.DropColumn(
                name: "LoggerBannerShowProtein",
                table: "FoodLogSettings");
        }
    }
}
