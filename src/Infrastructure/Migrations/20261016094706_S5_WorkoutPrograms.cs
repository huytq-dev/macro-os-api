using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S5_WorkoutPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseTargetsByCycle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutExercisePrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleIndex = table.Column<int>(type: "int", nullable: false),
                    TargetSets = table.Column<int>(type: "int", nullable: false),
                    RepMin = table.Column<int>(type: "int", nullable: false),
                    RepMax = table.Column<int>(type: "int", nullable: false),
                    Rir = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    LoadTargetKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    SetTypeTemplateJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseTargetsByCycle", x => x.Id);
                    table.CheckConstraint("CK_ExerciseTargetsByCycle_Ranges", "[CycleIndex] >= 1 AND [TargetSets] >= 0 AND [RepMin] >= 0 AND [RepMin] <= [RepMax] AND ([Rir] IS NULL OR [Rir] >= 0) AND ([LoadTargetKg] IS NULL OR [LoadTargetKg] >= 0)");
                });

            migrationBuilder.CreateTable(
                name: "WorkoutDayTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutProgramRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayIndex = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsRestDay = table.Column<bool>(type: "bit", nullable: false),
                    GymProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutDayTemplates", x => x.Id);
                    table.CheckConstraint("CK_WorkoutDayTemplates_DayIndex", "[DayIndex] >= 1");
                    table.ForeignKey(
                        name: "FK_WorkoutDayTemplates_GymProfiles_GymProfileId",
                        column: x => x.GymProfileId,
                        principalTable: "GymProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutExercisePrescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutDayTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SupersetGroup = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RestSeconds = table.Column<int>(type: "int", nullable: true),
                    ProgressionPolicy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExercisePrescriptions", x => x.Id);
                    table.CheckConstraint("CK_WorkoutExercisePrescriptions_Rest", "[RestSeconds] IS NULL OR [RestSeconds] >= 0");
                    table.ForeignKey(
                        name: "FK_WorkoutExercisePrescriptions_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutExercisePrescriptions_WorkoutDayTemplates_WorkoutDayTemplateId",
                        column: x => x.WorkoutDayTemplateId,
                        principalTable: "WorkoutDayTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutProgramRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNo = table.Column<int>(type: "int", nullable: false),
                    CycleCount = table.Column<int>(type: "int", nullable: false),
                    DaysPerCycle = table.Column<int>(type: "int", nullable: false),
                    PeriodizationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    GymProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GeneratorVersion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutProgramRevisions", x => x.Id);
                    table.CheckConstraint("CK_WorkoutProgramRevisions_Ranges", "[CycleCount] >= 1 AND [DaysPerCycle] >= 1 AND [RevisionNo] >= 1");
                    table.ForeignKey(
                        name: "FK_WorkoutProgramRevisions_GymProfiles_GymProfileId",
                        column: x => x.GymProfileId,
                        principalTable: "GymProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreationMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutPrograms_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutPrograms_WorkoutProgramRevisions_CurrentRevisionId",
                        column: x => x.CurrentRevisionId,
                        principalTable: "WorkoutProgramRevisions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseTargetsByCycle_WorkoutExercisePrescriptionId_CycleIndex",
                table: "ExerciseTargetsByCycle",
                columns: new[] { "WorkoutExercisePrescriptionId", "CycleIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutDayTemplates_GymProfileId",
                table: "WorkoutDayTemplates",
                column: "GymProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutDayTemplates_WorkoutProgramRevisionId_DayIndex",
                table: "WorkoutDayTemplates",
                columns: new[] { "WorkoutProgramRevisionId", "DayIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercisePrescriptions_ExerciseId",
                table: "WorkoutExercisePrescriptions",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercisePrescriptions_WorkoutDayTemplateId_SortOrder",
                table: "WorkoutExercisePrescriptions",
                columns: new[] { "WorkoutDayTemplateId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutProgramRevisions_GymProfileId",
                table: "WorkoutProgramRevisions",
                column: "GymProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutProgramRevisions_WorkoutProgramId_RevisionNo",
                table: "WorkoutProgramRevisions",
                columns: new[] { "WorkoutProgramId", "RevisionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPrograms_CurrentRevisionId",
                table: "WorkoutPrograms",
                column: "CurrentRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPrograms_UserId",
                table: "WorkoutPrograms",
                column: "UserId",
                unique: true,
                filter: "[Status] = 'Active' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPrograms_UserId_Status",
                table: "WorkoutPrograms",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseTargetsByCycle_WorkoutExercisePrescriptions_WorkoutExercisePrescriptionId",
                table: "ExerciseTargetsByCycle",
                column: "WorkoutExercisePrescriptionId",
                principalTable: "WorkoutExercisePrescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutDayTemplates_WorkoutProgramRevisions_WorkoutProgramRevisionId",
                table: "WorkoutDayTemplates",
                column: "WorkoutProgramRevisionId",
                principalTable: "WorkoutProgramRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutProgramRevisions_WorkoutPrograms_WorkoutProgramId",
                table: "WorkoutProgramRevisions",
                column: "WorkoutProgramId",
                principalTable: "WorkoutPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPrograms_WorkoutProgramRevisions_CurrentRevisionId",
                table: "WorkoutPrograms");

            migrationBuilder.DropTable(
                name: "ExerciseTargetsByCycle");

            migrationBuilder.DropTable(
                name: "WorkoutExercisePrescriptions");

            migrationBuilder.DropTable(
                name: "WorkoutDayTemplates");

            migrationBuilder.DropTable(
                name: "WorkoutProgramRevisions");

            migrationBuilder.DropTable(
                name: "WorkoutPrograms");
        }
    }
}
