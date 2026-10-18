using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S15_NutrientUnionAndFoodTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill 11 cột micro cũ của FoodItems -> FoodNutrients (nguồn chất duy nhất) TRƯỚC khi xoá cột. Chỉ chạy được khi danh mục chất đã có code tương ứng (đã seed từ trước).
            foreach (var (column, code) in new[]
            {
                ("FiberPer100g", "fiber_g"),
                ("SugarPer100g", "sugars_g"),
                ("SodiumPer100g", "sodium_mg"),
                ("PotassiumPer100g", "potassium_mg"),
                ("CalciumPer100g", "calcium_mg"),
                ("IronPer100g", "iron_mg"),
                ("VitaminCPer100g", "vitamin_c_mg"),
                ("VitaminDPer100g", "vitamin_d_mcg"),
                ("SaturatedFatPer100g", "fat_saturated_g"),
                ("TransFatPer100g", "fat_trans_g"),
                ("CholesterolPer100g", "cholesterol_mg"),
            })
            {
                migrationBuilder.Sql($@"INSERT INTO FoodNutrients (Id, FoodItemId, NutrientDefinitionId, AmountPer100g, AmountStatus, CreatedAt)
SELECT NEWID(), f.Id, d.Id, f.{column}, 'Measured', SYSDATETIMEOFFSET()
FROM FoodItems f JOIN NutrientDefinitions d ON d.Code = '{code}'
WHERE f.{column} IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM FoodNutrients n WHERE n.FoodItemId = f.Id AND n.NutrientDefinitionId = d.Id)");
            }

            migrationBuilder.DropTable(
                name: "FoodLogEntryMicros");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "NutritionDayAggregates");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FoodLogs");

            migrationBuilder.DropColumn(
                name: "StatusSource",
                table: "FoodLogs");

            migrationBuilder.DropColumn(
                name: "CalciumPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "CholesterolPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "FiberPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "IronPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "PotassiumPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "SaturatedFatPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "SodiumPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "SugarPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "TransFatPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "VitaminCPer100g",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "VitaminDPer100g",
                table: "FoodItems");

            migrationBuilder.AddColumn<bool>(
                name: "IsCore",
                table: "NutrientDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: true); // dòng cũ đều là chất core (EF sinh false)

            migrationBuilder.AddColumn<string>(
                name: "UsdaNumber",
                table: "NutrientDefinitions",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FoodImportRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Query = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Imported = table.Column<int>(type: "int", nullable: false),
                    Skipped = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodImportRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutrientDefinitions_UsdaNumber",
                table: "NutrientDefinitions",
                column: "UsdaNumber",
                unique: true,
                filter: "[UsdaNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FoodImportRuns_Provider_StartedAt",
                table: "FoodImportRuns",
                columns: new[] { "Provider", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodImportRuns");

            migrationBuilder.DropIndex(
                name: "IX_NutrientDefinitions_UsdaNumber",
                table: "NutrientDefinitions");

            migrationBuilder.DropColumn(
                name: "IsCore",
                table: "NutrientDefinitions");

            migrationBuilder.DropColumn(
                name: "UsdaNumber",
                table: "NutrientDefinitions");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "NutritionDayAggregates",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "FoodLogs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unmarked");

            migrationBuilder.AddColumn<string>(
                name: "StatusSource",
                table: "FoodLogs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.AddColumn<decimal>(
                name: "CalciumPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CholesterolPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FiberPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IronPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PotassiumPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaturatedFatPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SodiumPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SugarPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TransFatPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VitaminCPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VitaminDPer100g",
                table: "FoodItems",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FoodLogEntryMicros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodLogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Calcium = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Cholesterol = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Iron = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Potassium = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    SaturatedFat = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Sugar = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    TransFat = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    VitaminA = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    VitaminC = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    VitaminD = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogEntryMicros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodLogEntryMicros_FoodLogEntries_FoodLogEntryId",
                        column: x => x.FoodLogEntryId,
                        principalTable: "FoodLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntryMicros_FoodLogEntryId",
                table: "FoodLogEntryMicros",
                column: "FoodLogEntryId",
                unique: true);
        }
    }
}
