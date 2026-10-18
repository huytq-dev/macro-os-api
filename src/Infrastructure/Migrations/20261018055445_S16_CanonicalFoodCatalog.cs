using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S16_CanonicalFoodCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CanonicalFoodId",
                table: "FoodItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRepresentative",
                table: "FoodItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CanonicalFoodId",
                table: "FoodAliases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CanonicalFoods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameNormalized = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameVi = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DefaultServingGrams = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    DensityGPerMl = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalFoods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiIngredientCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CanonicalFoodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HitCount = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiIngredientCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiIngredientCaches_CanonicalFoods_CanonicalFoodId",
                        column: x => x.CanonicalFoodId,
                        principalTable: "CanonicalFoods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiIngredientCaches_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiIngredientCaches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodSourceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanonicalFoodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    MatchConfidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    MatchMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodSourceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodSourceRecords_CanonicalFoods_CanonicalFoodId",
                        column: x => x.CanonicalFoodId,
                        principalTable: "CanonicalFoods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FoodSourceRecords_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_FoodItems_Representative",
                table: "FoodItems",
                column: "CanonicalFoodId",
                unique: true,
                filter: "[IsRepresentative] = 1 AND [CanonicalFoodId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FoodAliases_CanonicalFoodId",
                table: "FoodAliases",
                column: "CanonicalFoodId");

            migrationBuilder.CreateIndex(
                name: "IX_AiIngredientCaches_CanonicalFoodId",
                table: "AiIngredientCaches",
                column: "CanonicalFoodId");

            migrationBuilder.CreateIndex(
                name: "IX_AiIngredientCaches_FoodItemId",
                table: "AiIngredientCaches",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AiIngredientCaches_UserId_Locale_NormalizedName",
                table: "AiIngredientCaches",
                columns: new[] { "UserId", "Locale", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalFoods_NameNormalized",
                table: "CanonicalFoods",
                column: "NameNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodSourceRecords_CanonicalFoodId_Priority",
                table: "FoodSourceRecords",
                columns: new[] { "CanonicalFoodId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_FoodSourceRecords_FoodItemId",
                table: "FoodSourceRecords",
                column: "FoodItemId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodAliases_CanonicalFoods_CanonicalFoodId",
                table: "FoodAliases",
                column: "CanonicalFoodId",
                principalTable: "CanonicalFoods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodItems_CanonicalFoods_CanonicalFoodId",
                table: "FoodItems",
                column: "CanonicalFoodId",
                principalTable: "CanonicalFoods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoodAliases_CanonicalFoods_CanonicalFoodId",
                table: "FoodAliases");

            migrationBuilder.DropForeignKey(
                name: "FK_FoodItems_CanonicalFoods_CanonicalFoodId",
                table: "FoodItems");

            migrationBuilder.DropTable(
                name: "AiIngredientCaches");

            migrationBuilder.DropTable(
                name: "FoodSourceRecords");

            migrationBuilder.DropTable(
                name: "CanonicalFoods");

            migrationBuilder.DropIndex(
                name: "UX_FoodItems_Representative",
                table: "FoodItems");

            migrationBuilder.DropIndex(
                name: "IX_FoodAliases_CanonicalFoodId",
                table: "FoodAliases");

            migrationBuilder.DropColumn(
                name: "CanonicalFoodId",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "IsRepresentative",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "CanonicalFoodId",
                table: "FoodAliases");
        }
    }
}
