using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FavoritePresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FoodFavorites_UserId_FoodItemId",
                table: "FoodFavorites");

            migrationBuilder.AddColumn<Guid>(
                name: "FoodServingId",
                table: "FoodFavorites",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "FoodFavorites",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "FoodFavorites",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FoodFavorites_FoodServingId",
                table: "FoodFavorites",
                column: "FoodServingId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodFavorites_UserId_FoodItemId_FoodServingId_Quantity",
                table: "FoodFavorites",
                columns: new[] { "UserId", "FoodItemId", "FoodServingId", "Quantity" },
                unique: true,
                filter: "[FoodServingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FoodFavorites_UserId_SortOrder",
                table: "FoodFavorites",
                columns: new[] { "UserId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_FoodFavorites_FoodServings_FoodServingId",
                table: "FoodFavorites",
                column: "FoodServingId",
                principalTable: "FoodServings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoodFavorites_FoodServings_FoodServingId",
                table: "FoodFavorites");

            migrationBuilder.DropIndex(
                name: "IX_FoodFavorites_FoodServingId",
                table: "FoodFavorites");

            migrationBuilder.DropIndex(
                name: "IX_FoodFavorites_UserId_FoodItemId_FoodServingId_Quantity",
                table: "FoodFavorites");

            migrationBuilder.DropIndex(
                name: "IX_FoodFavorites_UserId_SortOrder",
                table: "FoodFavorites");

            migrationBuilder.DropColumn(
                name: "FoodServingId",
                table: "FoodFavorites");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "FoodFavorites");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "FoodFavorites");

            migrationBuilder.CreateIndex(
                name: "IX_FoodFavorites_UserId_FoodItemId",
                table: "FoodFavorites",
                columns: new[] { "UserId", "FoodItemId" },
                unique: true);
        }
    }
}
