using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S11_FoodLogSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodLogSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Overages = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HourRangeFirst = table.Column<int>(type: "int", nullable: false),
                    HourRangeLast = table.Column<int>(type: "int", nullable: false),
                    Alignment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AddFoodsToHour = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShowTimestamps = table.Column<bool>(type: "bit", nullable: false),
                    ShowHourlyTotals = table.Column<bool>(type: "bit", nullable: false),
                    WeekBanner = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CalendarScrolling = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PremoveEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ShowBrandedResults = table.Column<bool>(type: "bit", nullable: false),
                    ShowOpenFoodFactsResults = table.Column<bool>(type: "bit", nullable: false),
                    Optimization = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogSettings", x => x.Id);
                    table.CheckConstraint("CK_FoodLogSettings_HourRange", "[HourRangeFirst] BETWEEN 0 AND 23 AND [HourRangeLast] BETWEEN 0 AND 23 AND [HourRangeLast] - [HourRangeFirst] >= 1");
                    table.ForeignKey(
                        name: "FK_FoodLogSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogSettings_UserId",
                table: "FoodLogSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodLogSettings");
        }
    }
}
