using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S8_FieldCompleteness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientSessionId",
                table: "WorkoutSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "WorkoutSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PausedSeconds",
                table: "WorkoutSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WorkoutSessions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "WorkoutSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "WorkoutSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkoutProgramId",
                table: "WorkoutSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalPrescriptionId",
                table: "WorkoutSessionExercises",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedExerciseId",
                table: "WorkoutSessionExercises",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplacementReason",
                table: "WorkoutSessionExercises",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupersetGroup",
                table: "WorkoutSessionExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkoutPrograms",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadType",
                table: "WorkoutExercisePrescriptions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Weighted");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxLoadKg",
                table: "WorkoutExercisePrescriptions",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinLoadKg",
                table: "WorkoutExercisePrescriptions",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tempo",
                table: "WorkoutExercisePrescriptions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "WeightEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MeasuredAt",
                table: "WeightEntries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WeightEntries",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "WeightEntries",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "TrendAlgorithmVersion",
                table: "WeightEntries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TrendConfidence",
                table: "WeightEntries",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CoachingRecommendationId",
                table: "WeeklyCheckIns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodEnd",
                table: "WeeklyCheckIns",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodStart",
                table: "WeeklyCheckIns",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "AllergiesJson",
                table: "UserProfiles",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DietaryPreferencesJson",
                table: "UserProfiles",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnergyUnit",
                table: "UserProfiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Kcal");

            migrationBuilder.AddColumn<string>(
                name: "LoggingMode",
                table: "UserProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.AddColumn<string>(
                name: "OnboardingState",
                table: "UserProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NotStarted");

            migrationBuilder.AddColumn<int>(
                name: "OnboardingStep",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserProfiles",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "TrainingExperience",
                table: "UserProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypicalMealTimesJson",
                table: "UserProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeekStartDay",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Monday");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentVersionId",
                table: "Recipes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Recipes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "Recipes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GramsNormalized",
                table: "RecipeIngredients",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutrientSnapshotJson",
                table: "RecipeIngredients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlgorithmVersion",
                table: "MacroProgramWeeks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveTo",
                table: "MacroProgramWeeks",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferencesJson",
                table: "MacroProgramWeeks",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisionNo",
                table: "MacroProgramWeeks",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "WeeklyBudgetKcal",
                table: "MacroProgramWeeks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalorieShiftDays",
                table: "MacroPrograms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CalorieShiftMode",
                table: "MacroPrograms",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Even");

            migrationBuilder.AddColumn<decimal>(
                name: "CalorieShiftPercent",
                table: "MacroPrograms",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MacroPrograms",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndedAt",
                table: "MacroPrograms",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GoalId",
                table: "MacroPrograms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "MacroPrograms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MacroPrograms",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "MacroPrograms",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Generated");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MacroPrograms",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "StrategyVersion",
                table: "MacroPrograms",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetWeightKg",
                table: "MacroPrograms",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalTargetsJson",
                table: "MacroProgramDayTargets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DayType",
                table: "MacroProgramDayTargets",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<string>(
                name: "LocationLabel",
                table: "GymProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MillilitersEquivalent",
                table: "FoodServings",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SizeValue",
                table: "FoodServings",
                type: "decimal(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "UnitType",
                table: "FoodServings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Household");

            migrationBuilder.AddColumn<decimal>(
                name: "AiConfidence",
                table: "FoodLogEntries",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LoggedAtUtc",
                table: "FoodLogEntries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FoodLogEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecipeVersionId",
                table: "FoodLogEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FoodLogEntries",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ServingSnapshotJson",
                table: "FoodLogEntries",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "FoodLogEntries",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Search");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "FoodItems",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DensityGPerMl",
                table: "FoodItems",
                type: "decimal(8,4)",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalFoodId",
                table: "FoodItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FoodItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUserEdited",
                table: "FoodItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "FoodItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalUnits",
                table: "FoodItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderCode",
                table: "FoodItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityStatus",
                table: "FoodItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unverified");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceCapturedAt",
                table: "FoodItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VerifiedAt",
                table: "FoodItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "FoodItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "Completeness",
                table: "ExpenditureSnapshots",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QualityScore",
                table: "ExpenditureSnapshots",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WindowEndDate",
                table: "ExpenditureSnapshots",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WindowStartDate",
                table: "ExpenditureSnapshots",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestSecondsOverride",
                table: "ExerciseTargetsByCycle",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetRpe",
                table: "ExerciseTargetsByCycle",
                type: "decimal(3,1)",
                precision: 3,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientSetId",
                table: "ExerciseSets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "ExerciseSets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSkipped",
                table: "ExerciseSets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ExerciseSets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "TargetSnapshotJson",
                table: "ExerciseSets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnilateral",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MeasurementType",
                table: "Exercises",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "WeightReps");

            migrationBuilder.AddColumn<string>(
                name: "MediaLicense",
                table: "Exercises",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MovementPattern",
                table: "Exercises",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<decimal>(
                name: "ContributionWeight",
                table: "ExerciseMuscles",
                type: "decimal(4,3)",
                precision: 4,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OldMacroProgramWeekId",
                table: "CoachingRecommendations",
                type: "uniqueidentifier",
                nullable: true);

            // Backfill dữ liệu cũ trước khi tạo unique index (MacroProgramId, RevisionNo)
            migrationBuilder.Sql("UPDATE MacroPrograms SET Status = 'Archived' WHERE IsActive = 0");
            migrationBuilder.Sql(@"
                ;WITH r AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY MacroProgramId ORDER BY EffectiveFrom, CreatedAt) AS rn FROM MacroProgramWeeks)
                UPDATE w SET RevisionNo = r.rn FROM MacroProgramWeeks w JOIN r ON r.Id = w.Id");
            migrationBuilder.Sql(@"
                UPDATE w SET EffectiveTo = DATEADD(day, -1, n.EffectiveFrom)
                FROM MacroProgramWeeks w
                CROSS APPLY (SELECT TOP 1 x.EffectiveFrom FROM MacroProgramWeeks x
                             WHERE x.MacroProgramId = w.MacroProgramId AND x.EffectiveFrom > w.EffectiveFrom
                             ORDER BY x.EffectiveFrom) n");
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId_ClientSessionId",
                table: "WorkoutSessions",
                columns: new[] { "UserId", "ClientSessionId" },
                unique: true,
                filter: "[ClientSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_WorkoutProgramId",
                table: "WorkoutSessions",
                column: "WorkoutProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionExercises_OriginalPrescriptionId",
                table: "WorkoutSessionExercises",
                column: "OriginalPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionExercises_ReplacedExerciseId",
                table: "WorkoutSessionExercises",
                column: "ReplacedExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightEntries_UserId_Source_ExternalId",
                table: "WeightEntries",
                columns: new[] { "UserId", "Source", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyCheckIns_CoachingRecommendationId",
                table: "WeeklyCheckIns",
                column: "CoachingRecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CurrentVersionId",
                table: "Recipes",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MacroProgramWeeks_MacroProgramId_RevisionNo",
                table: "MacroProgramWeeks",
                columns: new[] { "MacroProgramId", "RevisionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MacroPrograms_GoalId",
                table: "MacroPrograms",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntries_RecipeVersionId",
                table: "FoodLogEntries",
                column: "RecipeVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_Barcode_Country",
                table: "FoodItems",
                columns: new[] { "Barcode", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_ProviderCode_ExternalFoodId",
                table: "FoodItems",
                columns: new[] { "ProviderCode", "ExternalFoodId" },
                unique: true,
                filter: "[ProviderCode] IS NOT NULL AND [ExternalFoodId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSets_WorkoutSessionExerciseId_ClientSetId",
                table: "ExerciseSets",
                columns: new[] { "WorkoutSessionExerciseId", "ClientSetId" },
                unique: true,
                filter: "[ClientSetId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CoachingRecommendations_OldMacroProgramWeekId",
                table: "CoachingRecommendations",
                column: "OldMacroProgramWeekId");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachingRecommendations_MacroProgramWeeks_OldMacroProgramWeekId",
                table: "CoachingRecommendations",
                column: "OldMacroProgramWeekId",
                principalTable: "MacroProgramWeeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodLogEntries_RecipeVersions_RecipeVersionId",
                table: "FoodLogEntries",
                column: "RecipeVersionId",
                principalTable: "RecipeVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MacroPrograms_Goals_GoalId",
                table: "MacroPrograms",
                column: "GoalId",
                principalTable: "Goals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_RecipeVersions_CurrentVersionId",
                table: "Recipes",
                column: "CurrentVersionId",
                principalTable: "RecipeVersions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WeeklyCheckIns_CoachingRecommendations_CoachingRecommendationId",
                table: "WeeklyCheckIns",
                column: "CoachingRecommendationId",
                principalTable: "CoachingRecommendations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessionExercises_Exercises_ReplacedExerciseId",
                table: "WorkoutSessionExercises",
                column: "ReplacedExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessionExercises_WorkoutExercisePrescriptions_OriginalPrescriptionId",
                table: "WorkoutSessionExercises",
                column: "OriginalPrescriptionId",
                principalTable: "WorkoutExercisePrescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutPrograms_WorkoutProgramId",
                table: "WorkoutSessions",
                column: "WorkoutProgramId",
                principalTable: "WorkoutPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoachingRecommendations_MacroProgramWeeks_OldMacroProgramWeekId",
                table: "CoachingRecommendations");

            migrationBuilder.DropForeignKey(
                name: "FK_FoodLogEntries_RecipeVersions_RecipeVersionId",
                table: "FoodLogEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_MacroPrograms_Goals_GoalId",
                table: "MacroPrograms");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_RecipeVersions_CurrentVersionId",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_WeeklyCheckIns_CoachingRecommendations_CoachingRecommendationId",
                table: "WeeklyCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessionExercises_Exercises_ReplacedExerciseId",
                table: "WorkoutSessionExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessionExercises_WorkoutExercisePrescriptions_OriginalPrescriptionId",
                table: "WorkoutSessionExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutPrograms_WorkoutProgramId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_UserId_ClientSessionId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_WorkoutProgramId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessionExercises_OriginalPrescriptionId",
                table: "WorkoutSessionExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessionExercises_ReplacedExerciseId",
                table: "WorkoutSessionExercises");

            migrationBuilder.DropIndex(
                name: "IX_WeightEntries_UserId_Source_ExternalId",
                table: "WeightEntries");

            migrationBuilder.DropIndex(
                name: "IX_WeeklyCheckIns_CoachingRecommendationId",
                table: "WeeklyCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_CurrentVersionId",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_MacroProgramWeeks_MacroProgramId_RevisionNo",
                table: "MacroProgramWeeks");

            migrationBuilder.DropIndex(
                name: "IX_MacroPrograms_GoalId",
                table: "MacroPrograms");

            migrationBuilder.DropIndex(
                name: "IX_FoodLogEntries_RecipeVersionId",
                table: "FoodLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_FoodItems_Barcode_Country",
                table: "FoodItems");

            migrationBuilder.DropIndex(
                name: "IX_FoodItems_ProviderCode_ExternalFoodId",
                table: "FoodItems");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseSets_WorkoutSessionExerciseId_ClientSetId",
                table: "ExerciseSets");

            migrationBuilder.DropIndex(
                name: "IX_CoachingRecommendations_OldMacroProgramWeekId",
                table: "CoachingRecommendations");

            migrationBuilder.DropColumn(
                name: "ClientSessionId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "PausedSeconds",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "WorkoutProgramId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "OriginalPrescriptionId",
                table: "WorkoutSessionExercises");

            migrationBuilder.DropColumn(
                name: "ReplacedExerciseId",
                table: "WorkoutSessionExercises");

            migrationBuilder.DropColumn(
                name: "ReplacementReason",
                table: "WorkoutSessionExercises");

            migrationBuilder.DropColumn(
                name: "SupersetGroup",
                table: "WorkoutSessionExercises");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkoutPrograms");

            migrationBuilder.DropColumn(
                name: "LoadType",
                table: "WorkoutExercisePrescriptions");

            migrationBuilder.DropColumn(
                name: "MaxLoadKg",
                table: "WorkoutExercisePrescriptions");

            migrationBuilder.DropColumn(
                name: "MinLoadKg",
                table: "WorkoutExercisePrescriptions");

            migrationBuilder.DropColumn(
                name: "Tempo",
                table: "WorkoutExercisePrescriptions");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "WeightEntries");

            migrationBuilder.DropColumn(
                name: "MeasuredAt",
                table: "WeightEntries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WeightEntries");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "WeightEntries");

            migrationBuilder.DropColumn(
                name: "TrendAlgorithmVersion",
                table: "WeightEntries");

            migrationBuilder.DropColumn(
                name: "TrendConfidence",
                table: "WeightEntries");

            migrationBuilder.DropColumn(
                name: "CoachingRecommendationId",
                table: "WeeklyCheckIns");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "WeeklyCheckIns");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "WeeklyCheckIns");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AllergiesJson",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DietaryPreferencesJson",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "EnergyUnit",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LoggingMode",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "OnboardingState",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "OnboardingStep",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TrainingExperience",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TypicalMealTimesJson",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WeekStartDay",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CurrentVersionId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "GramsNormalized",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "NutrientSnapshotJson",
                table: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "AlgorithmVersion",
                table: "MacroProgramWeeks");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "MacroProgramWeeks");

            migrationBuilder.DropColumn(
                name: "PreferencesJson",
                table: "MacroProgramWeeks");

            migrationBuilder.DropColumn(
                name: "RevisionNo",
                table: "MacroProgramWeeks");

            migrationBuilder.DropColumn(
                name: "WeeklyBudgetKcal",
                table: "MacroProgramWeeks");

            migrationBuilder.DropColumn(
                name: "CalorieShiftDays",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "CalorieShiftMode",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "CalorieShiftPercent",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "GoalId",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "StrategyVersion",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "TargetWeightKg",
                table: "MacroPrograms");

            migrationBuilder.DropColumn(
                name: "AdditionalTargetsJson",
                table: "MacroProgramDayTargets");

            migrationBuilder.DropColumn(
                name: "DayType",
                table: "MacroProgramDayTargets");

            migrationBuilder.DropColumn(
                name: "LocationLabel",
                table: "GymProfiles");

            migrationBuilder.DropColumn(
                name: "MillilitersEquivalent",
                table: "FoodServings");

            migrationBuilder.DropColumn(
                name: "SizeValue",
                table: "FoodServings");

            migrationBuilder.DropColumn(
                name: "UnitType",
                table: "FoodServings");

            migrationBuilder.DropColumn(
                name: "AiConfidence",
                table: "FoodLogEntries");

            migrationBuilder.DropColumn(
                name: "LoggedAtUtc",
                table: "FoodLogEntries");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "FoodLogEntries");

            migrationBuilder.DropColumn(
                name: "RecipeVersionId",
                table: "FoodLogEntries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FoodLogEntries");

            migrationBuilder.DropColumn(
                name: "ServingSnapshotJson",
                table: "FoodLogEntries");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "FoodLogEntries");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "DensityGPerMl",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "ExternalFoodId",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "IsUserEdited",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "OriginalUnits",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "ProviderCode",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "SourceCapturedAt",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "Completeness",
                table: "ExpenditureSnapshots");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "ExpenditureSnapshots");

            migrationBuilder.DropColumn(
                name: "WindowEndDate",
                table: "ExpenditureSnapshots");

            migrationBuilder.DropColumn(
                name: "WindowStartDate",
                table: "ExpenditureSnapshots");

            migrationBuilder.DropColumn(
                name: "RestSecondsOverride",
                table: "ExerciseTargetsByCycle");

            migrationBuilder.DropColumn(
                name: "TargetRpe",
                table: "ExerciseTargetsByCycle");

            migrationBuilder.DropColumn(
                name: "ClientSetId",
                table: "ExerciseSets");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ExerciseSets");

            migrationBuilder.DropColumn(
                name: "IsSkipped",
                table: "ExerciseSets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ExerciseSets");

            migrationBuilder.DropColumn(
                name: "TargetSnapshotJson",
                table: "ExerciseSets");

            migrationBuilder.DropColumn(
                name: "IsUnilateral",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "MeasurementType",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "MediaLicense",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "MovementPattern",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ContributionWeight",
                table: "ExerciseMuscles");

            migrationBuilder.DropColumn(
                name: "OldMacroProgramWeekId",
                table: "CoachingRecommendations");
        }
    }
}
