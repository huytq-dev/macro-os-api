using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S10_FoodLogTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodLogNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NotedAt = table.Column<TimeOnly>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodLogNotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogNotes_UserId_LogDate",
                table: "FoodLogNotes",
                columns: new[] { "UserId", "LogDate" });

            // Timeline theo giờ: entry cũ chưa có giờ lấy giờ mặc định của bữa (khớp MealTimes.Default). Cột vẫn nullable.
            migrationBuilder.Sql(@"
                UPDATE FoodLogEntries SET EatenAt = CASE MealType
                    WHEN 'Breakfast' THEN '07:00:00'
                    WHEN 'MorningSnack' THEN '10:00:00'
                    WHEN 'Lunch' THEN '12:00:00'
                    WHEN 'AfternoonSnack' THEN '15:00:00'
                    WHEN 'Dinner' THEN '18:30:00'
                    ELSE '21:00:00' END
                WHERE EatenAt IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodLogNotes");
        }
    }
}
