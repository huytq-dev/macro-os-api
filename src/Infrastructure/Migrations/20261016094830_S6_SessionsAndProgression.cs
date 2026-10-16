using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S6_SessionsAndProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GymProfileId",
                table: "WorkoutSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkoutDayTemplateId",
                table: "WorkoutSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkoutProgramRevisionId",
                table: "WorkoutSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Metric = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(10,3)", precision: 10, scale: 3, nullable: false),
                    ExerciseSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalRecords_ExerciseSets_ExerciseSetId",
                        column: x => x.ExerciseSetId,
                        principalTable: "ExerciseSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonalRecords_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonalRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressionRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutExercisePrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReferenceSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestedWeightKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    SuggestedReps = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReasonCodesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressionRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressionRecommendations_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgressionRecommendations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgressionRecommendations_WorkoutExercisePrescriptions_WorkoutExercisePrescriptionId",
                        column: x => x.WorkoutExercisePrescriptionId,
                        principalTable: "WorkoutExercisePrescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgressionRecommendations_WorkoutSessions_ReferenceSessionId",
                        column: x => x.ReferenceSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutSessionExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PerceivedEffort = table.Column<int>(type: "int", nullable: true),
                    Pump = table.Column<int>(type: "int", nullable: true),
                    Soreness = table.Column<int>(type: "int", nullable: true),
                    Recovery = table.Column<int>(type: "int", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutFeedback", x => x.Id);
                    table.CheckConstraint("CK_WorkoutFeedback_Scale", "([PerceivedEffort] IS NULL OR [PerceivedEffort] BETWEEN 1 AND 10) AND ([Pump] IS NULL OR [Pump] BETWEEN 1 AND 10) AND ([Soreness] IS NULL OR [Soreness] BETWEEN 1 AND 10) AND ([Recovery] IS NULL OR [Recovery] BETWEEN 1 AND 10)");
                    table.ForeignKey(
                        name: "FK_WorkoutFeedback_WorkoutSessionExercises_WorkoutSessionExerciseId",
                        column: x => x.WorkoutSessionExerciseId,
                        principalTable: "WorkoutSessionExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutFeedback_WorkoutSessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutMuscleAggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    MuscleGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HardSets = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    VolumeKg = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SessionCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutMuscleAggregates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutMuscleAggregates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_GymProfileId",
                table: "WorkoutSessions",
                column: "GymProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_WorkoutDayTemplateId",
                table: "WorkoutSessions",
                column: "WorkoutDayTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_WorkoutProgramRevisionId",
                table: "WorkoutSessions",
                column: "WorkoutProgramRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalRecords_ExerciseId",
                table: "PersonalRecords",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalRecords_ExerciseSetId",
                table: "PersonalRecords",
                column: "ExerciseSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalRecords_UserId_ExerciseId_Metric",
                table: "PersonalRecords",
                columns: new[] { "UserId", "ExerciseId", "Metric" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRecommendations_ExerciseId",
                table: "ProgressionRecommendations",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRecommendations_ReferenceSessionId",
                table: "ProgressionRecommendations",
                column: "ReferenceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRecommendations_UserId_ExerciseId_CreatedAt",
                table: "ProgressionRecommendations",
                columns: new[] { "UserId", "ExerciseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressionRecommendations_WorkoutExercisePrescriptionId",
                table: "ProgressionRecommendations",
                column: "WorkoutExercisePrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedback_WorkoutSessionExerciseId",
                table: "WorkoutFeedback",
                column: "WorkoutSessionExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutFeedback_WorkoutSessionId",
                table: "WorkoutFeedback",
                column: "WorkoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutMuscleAggregates_UserId_PeriodStart_MuscleGroup",
                table: "WorkoutMuscleAggregates",
                columns: new[] { "UserId", "PeriodStart", "MuscleGroup" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_GymProfiles_GymProfileId",
                table: "WorkoutSessions",
                column: "GymProfileId",
                principalTable: "GymProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutDayTemplates_WorkoutDayTemplateId",
                table: "WorkoutSessions",
                column: "WorkoutDayTemplateId",
                principalTable: "WorkoutDayTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutProgramRevisions_WorkoutProgramRevisionId",
                table: "WorkoutSessions",
                column: "WorkoutProgramRevisionId",
                principalTable: "WorkoutProgramRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_GymProfiles_GymProfileId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutDayTemplates_WorkoutDayTemplateId",
                table: "WorkoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutProgramRevisions_WorkoutProgramRevisionId",
                table: "WorkoutSessions");

            migrationBuilder.DropTable(
                name: "PersonalRecords");

            migrationBuilder.DropTable(
                name: "ProgressionRecommendations");

            migrationBuilder.DropTable(
                name: "WorkoutFeedback");

            migrationBuilder.DropTable(
                name: "WorkoutMuscleAggregates");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_GymProfileId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_WorkoutDayTemplateId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_WorkoutProgramRevisionId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "GymProfileId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "WorkoutDayTemplateId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "WorkoutProgramRevisionId",
                table: "WorkoutSessions");
        }
    }
}
