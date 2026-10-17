using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S9_DashboardAndMicros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReferenceTarget",
                table: "NutrientDefinitions",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StartWeightKg",
                table: "Goals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLogged",
                table: "DailyProgressSnapshots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Dòng cũ chưa có cờ: suy ra từ kcal; rebuild kế tiếp sẽ ghi lại chính xác
            migrationBuilder.Sql("UPDATE DailyProgressSnapshots SET IsLogged = 1 WHERE CaloriesConsumed > 0");

            migrationBuilder.CreateTable(
                name: "DashboardWidgetSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WidgetKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardWidgetSettings", x => x.Id);
                    table.CheckConstraint("CK_DashboardWidgetSettings_SortOrder", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_DashboardWidgetSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Steps = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepEntries", x => x.Id);
                    table.CheckConstraint("CK_StepEntries_Steps", "[Steps] >= 0");
                    table.ForeignKey(
                        name: "FK_StepEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNutrientTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NutrientDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Floor = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    Target = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    Ceiling = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    PinOrder = table.Column<byte>(type: "tinyint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNutrientTargets", x => x.Id);
                    table.CheckConstraint("CK_UserNutrientTargets_PinOrder", "[PinOrder] IS NULL OR [PinOrder] BETWEEN 1 AND 8");
                    table.CheckConstraint("CK_UserNutrientTargets_Range", "([Floor] IS NULL OR [Target] IS NULL OR [Floor] <= [Target]) AND ([Target] IS NULL OR [Ceiling] IS NULL OR [Target] <= [Ceiling]) AND ([Floor] IS NULL OR [Ceiling] IS NULL OR [Floor] <= [Ceiling])");
                    table.ForeignKey(
                        name: "FK_UserNutrientTargets_NutrientDefinitions_NutrientDefinitionId",
                        column: x => x.NutrientDefinitionId,
                        principalTable: "NutrientDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserNutrientTargets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgetSettings_UserId_WidgetKey",
                table: "DashboardWidgetSettings",
                columns: new[] { "UserId", "WidgetKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StepEntries_UserId_LogDate",
                table: "StepEntries",
                columns: new[] { "UserId", "LogDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNutrientTargets_NutrientDefinitionId",
                table: "UserNutrientTargets",
                column: "NutrientDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNutrientTargets_UserId_NutrientDefinitionId",
                table: "UserNutrientTargets",
                columns: new[] { "UserId", "NutrientDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNutrientTargets_UserId_PinOrder",
                table: "UserNutrientTargets",
                columns: new[] { "UserId", "PinOrder" },
                unique: true,
                filter: "[PinOrder] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardWidgetSettings");

            migrationBuilder.DropTable(
                name: "StepEntries");

            migrationBuilder.DropTable(
                name: "UserNutrientTargets");

            migrationBuilder.DropColumn(
                name: "ReferenceTarget",
                table: "NutrientDefinitions");

            migrationBuilder.DropColumn(
                name: "StartWeightKg",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "IsLogged",
                table: "DailyProgressSnapshots");
        }
    }
}
