using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S4_NutritionStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlgorithmVersion",
                table: "ExpenditureSnapshots",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "1");

            migrationBuilder.AddColumn<string>(
                name: "ReasonCodesJson",
                table: "ExpenditureSnapshots",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoachingRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacroProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    ProposedTargetsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReasonCodesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachingRecommendations_MacroPrograms_MacroProgramId",
                        column: x => x.MacroProgramId,
                        principalTable: "MacroPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachingRecommendations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NutritionDayAggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Calories = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Protein = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Carbs = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Fat = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Fiber = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    NutrientsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LastCalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionDayAggregates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionDayAggregates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NutritionTargetOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CaloriesKcal = table.Column<int>(type: "int", nullable: false),
                    ProteinG = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CarbsG = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    FatG = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionTargetOverrides", x => x.Id);
                    table.CheckConstraint("CK_NutritionTargetOverrides_NonNegative", "[CaloriesKcal] >= 0 AND [ProteinG] >= 0 AND [CarbsG] >= 0 AND [FatG] >= 0");
                    table.ForeignKey(
                        name: "FK_NutritionTargetOverrides_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecalculationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cause = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    State = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecalculationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecalculationJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheckInDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoachingRecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedMacroProgramWeekId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckInDecisions_CoachingRecommendations_CoachingRecommendationId",
                        column: x => x.CoachingRecommendationId,
                        principalTable: "CoachingRecommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckInDecisions_MacroProgramWeeks_CreatedMacroProgramWeekId",
                        column: x => x.CreatedMacroProgramWeekId,
                        principalTable: "MacroProgramWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CheckInDecisions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInDecisions_CoachingRecommendationId",
                table: "CheckInDecisions",
                column: "CoachingRecommendationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckInDecisions_CreatedMacroProgramWeekId",
                table: "CheckInDecisions",
                column: "CreatedMacroProgramWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInDecisions_UserId",
                table: "CheckInDecisions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachingRecommendations_MacroProgramId_PeriodStart_Type_AlgorithmVersion",
                table: "CoachingRecommendations",
                columns: new[] { "MacroProgramId", "PeriodStart", "Type", "AlgorithmVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachingRecommendations_UserId_Status",
                table: "CoachingRecommendations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionDayAggregates_UserId_LocalDate",
                table: "NutritionDayAggregates",
                columns: new[] { "UserId", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NutritionTargetOverrides_UserId_LocalDate",
                table: "NutritionTargetOverrides",
                columns: new[] { "UserId", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecalculationJobs_State_CreatedAt",
                table: "RecalculationJobs",
                columns: new[] { "State", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecalculationJobs_UserId_FromDate_ToDate",
                table: "RecalculationJobs",
                columns: new[] { "UserId", "FromDate", "ToDate" },
                unique: true,
                filter: "[State] = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckInDecisions");

            migrationBuilder.DropTable(
                name: "NutritionDayAggregates");

            migrationBuilder.DropTable(
                name: "NutritionTargetOverrides");

            migrationBuilder.DropTable(
                name: "RecalculationJobs");

            migrationBuilder.DropTable(
                name: "CoachingRecommendations");

            migrationBuilder.DropColumn(
                name: "AlgorithmVersion",
                table: "ExpenditureSnapshots");

            migrationBuilder.DropColumn(
                name: "ReasonCodesJson",
                table: "ExpenditureSnapshots");
        }
    }
}
