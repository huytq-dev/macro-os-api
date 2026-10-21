using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S24_NutrientGoalHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNutrientTargetHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NutrientDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    Floor = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    Target = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    Ceiling = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNutrientTargetHistories", x => x.Id);
                    table.CheckConstraint("CK_UserNutrientTargetHistories_Range", "([Floor] IS NULL OR [Target] IS NULL OR [Floor] <= [Target]) AND ([Target] IS NULL OR [Ceiling] IS NULL OR [Target] <= [Ceiling]) AND ([Floor] IS NULL OR [Ceiling] IS NULL OR [Floor] <= [Ceiling])");
                    table.ForeignKey(
                        name: "FK_UserNutrientTargetHistories_NutrientDefinitions_NutrientDefinitionId",
                        column: x => x.NutrientDefinitionId,
                        principalTable: "NutrientDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserNutrientTargetHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNutrientTargetHistories_NutrientDefinitionId",
                table: "UserNutrientTargetHistories",
                column: "NutrientDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNutrientTargetHistories_UserId_NutrientDefinitionId_EffectiveFrom",
                table: "UserNutrientTargetHistories",
                columns: new[] { "UserId", "NutrientDefinitionId", "EffectiveFrom" },
                unique: true);

            // Backfill: mỗi dòng đang có floor/target/ceiling → một dòng lịch sử có hiệu lực từ ngày tạo dòng (CreatedAt, ngày theo offset đã lưu) để ngày cũ giữ đúng mức đang dùng.
            // Dòng chỉ có PinOrder (cả ba mức null) không sinh lịch sử.
            migrationBuilder.Sql(@"
INSERT INTO UserNutrientTargetHistories (Id, UserId, NutrientDefinitionId, EffectiveFrom, Floor, Target, Ceiling, CreatedAt)
SELECT NEWID(), UserId, NutrientDefinitionId, CAST(CreatedAt AS date), Floor, Target, Ceiling, SYSDATETIMEOFFSET()
FROM UserNutrientTargets
WHERE Floor IS NOT NULL OR Target IS NOT NULL OR Ceiling IS NOT NULL;");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserNutrientTargets_Range",
                table: "UserNutrientTargets");

            migrationBuilder.DropColumn(
                name: "Ceiling",
                table: "UserNutrientTargets");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "UserNutrientTargets");

            migrationBuilder.DropColumn(
                name: "Target",
                table: "UserNutrientTargets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Ceiling",
                table: "UserNutrientTargets",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Floor",
                table: "UserNutrientTargets",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Target",
                table: "UserNutrientTargets",
                type: "decimal(12,4)",
                precision: 12,
                scale: 4,
                nullable: true);

            // Hoàn lại mức hiện hành (dòng lịch sử mới nhất của mỗi nutrient; dòng rỗng = mặc định thì bỏ qua)
            migrationBuilder.Sql(@"
WITH latest AS (
    SELECT UserId, NutrientDefinitionId, Floor, Target, Ceiling,
           ROW_NUMBER() OVER (PARTITION BY UserId, NutrientDefinitionId ORDER BY EffectiveFrom DESC) AS rn
    FROM UserNutrientTargetHistories)
UPDATE t SET t.Floor = l.Floor, t.Target = l.Target, t.Ceiling = l.Ceiling
FROM UserNutrientTargets t
JOIN latest l ON l.rn = 1 AND l.UserId = t.UserId AND l.NutrientDefinitionId = t.NutrientDefinitionId;

WITH latest AS (
    SELECT UserId, NutrientDefinitionId, Floor, Target, Ceiling,
           ROW_NUMBER() OVER (PARTITION BY UserId, NutrientDefinitionId ORDER BY EffectiveFrom DESC) AS rn
    FROM UserNutrientTargetHistories)
INSERT INTO UserNutrientTargets (Id, UserId, NutrientDefinitionId, Floor, Target, Ceiling, PinOrder, CreatedAt)
SELECT NEWID(), l.UserId, l.NutrientDefinitionId, l.Floor, l.Target, l.Ceiling, NULL, SYSDATETIMEOFFSET()
FROM latest l
WHERE l.rn = 1 AND (l.Floor IS NOT NULL OR l.Target IS NOT NULL OR l.Ceiling IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM UserNutrientTargets t WHERE t.UserId = l.UserId AND t.NutrientDefinitionId = l.NutrientDefinitionId);");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserNutrientTargets_Range",
                table: "UserNutrientTargets",
                sql: "([Floor] IS NULL OR [Target] IS NULL OR [Floor] <= [Target]) AND ([Target] IS NULL OR [Ceiling] IS NULL OR [Target] <= [Ceiling]) AND ([Floor] IS NULL OR [Ceiling] IS NULL OR [Floor] <= [Ceiling])");

            migrationBuilder.DropTable(
                name: "UserNutrientTargetHistories");
        }
    }
}
