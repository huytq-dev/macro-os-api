using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FoodServingDefaultIndexFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FoodServings_FoodItemId_IsDefault",
                table: "FoodServings");

            migrationBuilder.CreateIndex(
                name: "IX_FoodServings_FoodItemId_IsDefault",
                table: "FoodServings",
                columns: new[] { "FoodItemId", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FoodServings_FoodItemId_IsDefault",
                table: "FoodServings");

            migrationBuilder.CreateIndex(
                name: "IX_FoodServings_FoodItemId_IsDefault",
                table: "FoodServings",
                columns: new[] { "FoodItemId", "IsDefault" },
                unique: true);
        }
    }
}
