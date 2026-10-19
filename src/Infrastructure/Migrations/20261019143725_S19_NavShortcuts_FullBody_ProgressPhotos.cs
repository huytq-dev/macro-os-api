using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S19_NavShortcuts_FullBody_ProgressPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ProgressPhotos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "PhotoDate",
                table: "ProgressPhotos",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<decimal>(
                name: "BustCm",
                table: "BodyMetrics",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LeftAnkleCm",
                table: "BodyMetrics",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LeftForearmCm",
                table: "BodyMetrics",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LeftWristCm",
                table: "BodyMetrics",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RightAnkleCm",
                table: "BodyMetrics",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RightForearmCm",
                table: "BodyMetrics",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RightWristCm",
                table: "BodyMetrics",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShouldersCm",
                table: "BodyMetrics",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserShortcutSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolbarStyle = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ShortcutsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserShortcutSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserShortcutSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressPhotos_UserId_PhotoDate_Angle",
                table: "ProgressPhotos",
                columns: new[] { "UserId", "PhotoDate", "Angle" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserShortcutSettings_UserId",
                table: "UserShortcutSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserShortcutSettings");

            migrationBuilder.DropIndex(
                name: "IX_ProgressPhotos_UserId_PhotoDate_Angle",
                table: "ProgressPhotos");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ProgressPhotos");

            migrationBuilder.DropColumn(
                name: "PhotoDate",
                table: "ProgressPhotos");

            migrationBuilder.DropColumn(
                name: "BustCm",
                table: "BodyMetrics");

            migrationBuilder.DropColumn(
                name: "LeftAnkleCm",
                table: "BodyMetrics");

            migrationBuilder.DropColumn(
                name: "LeftForearmCm",
                table: "BodyMetrics");

            migrationBuilder.DropColumn(
                name: "LeftWristCm",
                table: "BodyMetrics");

            migrationBuilder.DropColumn(
                name: "RightAnkleCm",
                table: "BodyMetrics");

            migrationBuilder.DropColumn(
                name: "RightForearmCm",
                table: "BodyMetrics");

            migrationBuilder.DropColumn(
                name: "RightWristCm",
                table: "BodyMetrics");

            migrationBuilder.DropColumn(
                name: "ShouldersCm",
                table: "BodyMetrics");
        }
    }
}
