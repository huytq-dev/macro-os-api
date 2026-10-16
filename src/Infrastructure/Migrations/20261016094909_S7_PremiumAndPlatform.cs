using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S7_PremiumAndPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrossDomainRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossDomainRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrossDomainRecommendations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                    table.CheckConstraint("CK_MealPlans_Dates", "[EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_MealPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedMeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FoodServingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    LoggedFoodLogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedMeals", x => x.Id);
                    table.CheckConstraint("CK_PlannedMeals_FoodOrRecipe", "([FoodItemId] IS NOT NULL AND [RecipeId] IS NULL) OR ([FoodItemId] IS NULL AND [RecipeId] IS NOT NULL AND [FoodServingId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_PlannedMeals_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlannedMeals_FoodLogEntries_LoggedFoodLogEntryId",
                        column: x => x.LoggedFoodLogEntryId,
                        principalTable: "FoodLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlannedMeals_FoodServings_FoodServingId",
                        column: x => x.FoodServingId,
                        principalTable: "FoodServings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlannedMeals_MealPlans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalTable: "MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedMeals_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrossDomainRecommendations_UserId_Status",
                table: "CrossDomainRecommendations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_UserId_RequestedAt",
                table: "ExportJobs",
                columns: new[] { "UserId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_UserId_StartDate",
                table: "MealPlans",
                columns: new[] { "UserId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMeals_FoodItemId",
                table: "PlannedMeals",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMeals_FoodServingId",
                table: "PlannedMeals",
                column: "FoodServingId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMeals_LoggedFoodLogEntryId",
                table: "PlannedMeals",
                column: "LoggedFoodLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMeals_MealPlanId_PlannedDate",
                table: "PlannedMeals",
                columns: new[] { "MealPlanId", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMeals_RecipeId",
                table: "PlannedMeals",
                column: "RecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrossDomainRecommendations");

            migrationBuilder.DropTable(
                name: "ExportJobs");

            migrationBuilder.DropTable(
                name: "PlannedMeals");

            migrationBuilder.DropTable(
                name: "MealPlans");
        }
    }
}
