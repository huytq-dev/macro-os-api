using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S3_NutritionLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FoodLogs",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

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

            migrationBuilder.AddColumn<Guid>(
                name: "FoodLogBatchId",
                table: "FoodLogEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FoodLogBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodLogBatches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodLogEntryNutrients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodLogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NutrientDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    AmountStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogEntryNutrients", x => x.Id);
                    table.CheckConstraint("CK_FoodLogEntryNutrients_AmountNonNegative", "[Amount] IS NULL OR [Amount] >= 0");
                    table.ForeignKey(
                        name: "FK_FoodLogEntryNutrients_FoodLogEntries_FoodLogEntryId",
                        column: x => x.FoodLogEntryId,
                        principalTable: "FoodLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FoodLogEntryNutrients_NutrientDefinitions_NutrientDefinitionId",
                        column: x => x.NutrientDefinitionId,
                        principalTable: "NutrientDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FoodScanAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StorageRef = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModelVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodScanAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodScanAnalyses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealTemplates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    Servings = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CookedYieldGrams = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    RawTotalGrams = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IngredientsSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalCalories = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalProtein = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalCarbs = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalFat = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeVersions_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FoodServingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealTemplateItems", x => x.Id);
                    table.CheckConstraint("CK_MealTemplateItems_FoodOrRecipe", "([FoodItemId] IS NOT NULL AND [RecipeId] IS NULL) OR ([FoodItemId] IS NULL AND [RecipeId] IS NOT NULL AND [FoodServingId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_MealTemplateItems_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealTemplateItems_FoodServings_FoodServingId",
                        column: x => x.FoodServingId,
                        principalTable: "FoodServings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealTemplateItems_MealTemplates_MealTemplateId",
                        column: x => x.MealTemplateId,
                        principalTable: "MealTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealTemplateItems_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreparedRecipeBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualYieldGrams = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PreparedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparedRecipeBatches", x => x.Id);
                    table.CheckConstraint("CK_PreparedRecipeBatches_YieldPositive", "[ActualYieldGrams] > 0");
                    table.ForeignKey(
                        name: "FK_PreparedRecipeBatches_RecipeVersions_RecipeVersionId",
                        column: x => x.RecipeVersionId,
                        principalTable: "RecipeVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreparedRecipeBatches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntries_FoodLogBatchId",
                table: "FoodLogEntries",
                column: "FoodLogBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogBatches_UserId_IdempotencyKey",
                table: "FoodLogBatches",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntryNutrients_FoodLogEntryId_NutrientDefinitionId",
                table: "FoodLogEntryNutrients",
                columns: new[] { "FoodLogEntryId", "NutrientDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntryNutrients_NutrientDefinitionId",
                table: "FoodLogEntryNutrients",
                column: "NutrientDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodScanAnalyses_ExpiresAt",
                table: "FoodScanAnalyses",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_FoodScanAnalyses_UserId_CreatedAt",
                table: "FoodScanAnalyses",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MealTemplateItems_FoodItemId",
                table: "MealTemplateItems",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTemplateItems_FoodServingId",
                table: "MealTemplateItems",
                column: "FoodServingId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTemplateItems_MealTemplateId",
                table: "MealTemplateItems",
                column: "MealTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTemplateItems_RecipeId",
                table: "MealTemplateItems",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_MealTemplates_UserId_Name",
                table: "MealTemplates",
                columns: new[] { "UserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PreparedRecipeBatches_RecipeVersionId",
                table: "PreparedRecipeBatches",
                column: "RecipeVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PreparedRecipeBatches_UserId_PreparedAt",
                table: "PreparedRecipeBatches",
                columns: new[] { "UserId", "PreparedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeVersions_RecipeId_VersionNo",
                table: "RecipeVersions",
                columns: new[] { "RecipeId", "VersionNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodLogEntries_FoodLogBatches_FoodLogBatchId",
                table: "FoodLogEntries",
                column: "FoodLogBatchId",
                principalTable: "FoodLogBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoodLogEntries_FoodLogBatches_FoodLogBatchId",
                table: "FoodLogEntries");

            migrationBuilder.DropTable(
                name: "FoodLogBatches");

            migrationBuilder.DropTable(
                name: "FoodLogEntryNutrients");

            migrationBuilder.DropTable(
                name: "FoodScanAnalyses");

            migrationBuilder.DropTable(
                name: "MealTemplateItems");

            migrationBuilder.DropTable(
                name: "PreparedRecipeBatches");

            migrationBuilder.DropTable(
                name: "MealTemplates");

            migrationBuilder.DropTable(
                name: "RecipeVersions");

            migrationBuilder.DropIndex(
                name: "IX_FoodLogEntries_FoodLogBatchId",
                table: "FoodLogEntries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FoodLogs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FoodLogs");

            migrationBuilder.DropColumn(
                name: "StatusSource",
                table: "FoodLogs");

            migrationBuilder.DropColumn(
                name: "FoodLogBatchId",
                table: "FoodLogEntries");
        }
    }
}
