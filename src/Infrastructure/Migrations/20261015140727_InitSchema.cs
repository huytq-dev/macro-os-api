using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    PrimaryMuscle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Equipment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCustom = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CaloriesPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ProteinPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CarbsPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    FatPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    FiberPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    SugarPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    SodiumPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    PotassiumPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    CalciumPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    IronPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    VitaminCPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    VitaminDPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    SaturatedFatPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    TransFatPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    CholesterolPer100g = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PriceMonthly = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PriceYearly = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MaxCustomFoods = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    MaxCustomWorkouts = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    HasBarcodeScanning = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasAdvancedAnalytics = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasNutritionistAccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasAiCoach = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RefreshTokenExpiry = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Difficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    WorkoutGoal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseMuscles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MuscleGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseMuscles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseMuscles_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodServings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WeightInGrams = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodServings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodServings_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodyMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasuredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MeasuredTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Bmi = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    BodyFatPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MuscleMassKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    BoneMassKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WaterPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    VisceralFatLevel = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    WaistCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    HipCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ChestCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NeckCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    LeftArmCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    RightArmCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    LeftThighCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    RightThighCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    LeftCalfCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    RightCalfCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    RestingHeartRate = table.Column<int>(type: "int", nullable: true),
                    BloodPressureSystolic = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    BloodPressureDiastolic = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    FrontPhotoUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    SidePhotoUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    BackPhotoUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodyMetrics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenditureSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EstimatedTdeeKcal = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AvgCaloriesConsumed = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    WeightChangeRateKgPerWeek = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: true),
                    WindowDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenditureSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenditureSnapshots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FoodLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalCalories = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TotalProtein = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TotalCarbs = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TotalFat = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TotalFiber = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TotalWater = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SyncSteps = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SyncWeight = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SyncWorkouts = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ExternalUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthIntegrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoggingStreaks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StreakType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
                    LongestStreak = table.Column<int>(type: "int", nullable: false),
                    LastLogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoggingStreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoggingStreaks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MacroPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GoalType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DietStyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WeeklyWeightChangeTargetPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    WeeklyWeightChangeTargetKg = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    ProteinLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProteinGPerKgBodyweight = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    FixedProteinGrams = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    CalorieFloorKcal = table.Column<int>(type: "int", nullable: true),
                    CheckInWeekday = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpenditureStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ManualInitialTdeeKcal = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MacroPrograms_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MetaData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeriodLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CycleLengthDays = table.Column<int>(type: "int", nullable: true),
                    PeriodLengthDays = table.Column<int>(type: "int", nullable: true),
                    Symptoms = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ServingsCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    PrepTimeMinutes = table.Column<int>(type: "int", nullable: true),
                    CookTimeMinutes = table.Column<int>(type: "int", nullable: true),
                    MealType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalCalories = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TotalProtein = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TotalCarbs = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TotalFat = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recipes_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReminderSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReminderTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    DaysOfWeek = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReminderSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UnitPreference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Metric"),
                    HeightCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ActivityLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrainingType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "None"),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Locale = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalSubscriptionId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeightEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TrendWeightKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "InProgress"),
                    CaloriesBurned = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    PerceivedExertion = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MoodBefore = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MoodAfter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessions", x => x.Id);
                    table.CheckConstraint("CK_WorkoutSessions_PerceivedExertion", "[PerceivedExertion] IS NULL OR ([PerceivedExertion] >= 1 AND [PerceivedExertion] <= 10)");
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_WorkoutTemplates_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutTemplateExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DefaultSets = table.Column<int>(type: "int", nullable: false),
                    DefaultReps = table.Column<int>(type: "int", nullable: true),
                    DefaultWeight = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    DefaultDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    DefaultDistance = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    RestSeconds = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutTemplateExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutTemplateExercises_WorkoutTemplates_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaterLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountMl = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    LoggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaterLogs_FoodLogs_FoodLogId",
                        column: x => x.FoodLogId,
                        principalTable: "FoodLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WaterLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MacroProgramDayTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacroProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsFastingDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CaloriesTarget = table.Column<int>(type: "int", nullable: true),
                    ProteinTarget = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    CarbsTarget = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    FatTarget = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroProgramDayTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MacroProgramDayTargets_MacroPrograms_MacroProgramId",
                        column: x => x.MacroProgramId,
                        principalTable: "MacroPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MacroProgramWeeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MacroProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    CaloriesTarget = table.Column<int>(type: "int", nullable: false),
                    ProteinTarget = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CarbsTarget = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    FatTarget = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    AdjustmentReason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousCaloriesTarget = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroProgramWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MacroProgramWeeks_MacroPrograms_MacroProgramId",
                        column: x => x.MacroProgramId,
                        principalTable: "MacroPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EatenAt = table.Column<TimeOnly>(type: "time", nullable: true),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FoodServingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Calories = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Protein = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Carbs = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Fat = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Fiber = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Sodium = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogEntries", x => x.Id);
                    table.CheckConstraint("CK_FoodLogEntries_FoodOrRecipe", "([FoodItemId] IS NOT NULL AND [RecipeId] IS NULL) OR ([FoodItemId] IS NULL AND [RecipeId] IS NOT NULL AND [FoodServingId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_FoodLogEntries_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FoodLogEntries_FoodLogs_FoodLogId",
                        column: x => x.FoodLogId,
                        principalTable: "FoodLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FoodLogEntries_FoodServings_FoodServingId",
                        column: x => x.FoodServingId,
                        principalTable: "FoodServings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FoodLogEntries_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodServingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_FoodServings_FoodServingId",
                        column: x => x.FoodServingId,
                        principalTable: "FoodServings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSessionExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RestSeconds = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessionExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutSessionExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutSessionExercises_WorkoutSessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "WorkoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyProgressSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CaloriesConsumed = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CaloriesTarget = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ProteinConsumed = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CarbsConsumed = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    FatConsumed = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    WaterConsumed = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    WorkoutCount = table.Column<int>(type: "int", nullable: false),
                    TotalWorkoutMinutes = table.Column<int>(type: "int", nullable: false),
                    CaloriesBurned = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    StepsCount = table.Column<int>(type: "int", nullable: true),
                    TrendWeightKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MacroProgramWeekId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyProgressSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyProgressSnapshots_MacroProgramWeeks_MacroProgramWeekId",
                        column: x => x.MacroProgramWeekId,
                        principalTable: "MacroProgramWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DailyProgressSnapshots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WeeklyCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MacroProgramWeekId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TdeeEstimate = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    TrendWeightKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WeightChangeRateKgPerWeek = table.Column<decimal>(type: "decimal(5,3)", precision: 5, scale: 3, nullable: true),
                    PreviousCaloriesTarget = table.Column<int>(type: "int", nullable: false),
                    NewCaloriesTarget = table.Column<int>(type: "int", nullable: false),
                    CaloriesDelta = table.Column<int>(type: "int", nullable: false),
                    Accepted = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    WeeklyBudgetKcal = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyCheckIns_MacroProgramWeeks_MacroProgramWeekId",
                        column: x => x.MacroProgramWeekId,
                        principalTable: "MacroProgramWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyCheckIns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FoodLogEntryMicros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodLogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sugar = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    SaturatedFat = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    TransFat = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Cholesterol = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Potassium = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    VitaminA = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    VitaminC = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    VitaminD = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Calcium = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    Iron = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogEntryMicros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodLogEntryMicros_FoodLogEntries_FoodLogEntryId",
                        column: x => x.FoodLogEntryId,
                        principalTable: "FoodLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutSessionExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SetNumber = table.Column<int>(type: "int", nullable: false),
                    SetType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reps = table.Column<int>(type: "int", nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    DistanceKm = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: true),
                    Rpm = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Rir = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    Rpe = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSets", x => x.Id);
                    table.CheckConstraint("CK_ExerciseSets_Rpe", "[Rpe] IS NULL OR ([Rpe] >= 1 AND [Rpe] <= 10)");
                    table.ForeignKey(
                        name: "FK_ExerciseSets_WorkoutSessionExercises_WorkoutSessionExerciseId",
                        column: x => x.WorkoutSessionExerciseId,
                        principalTable: "WorkoutSessionExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BodyMetrics_UserId_MeasuredDate",
                table: "BodyMetrics",
                columns: new[] { "UserId", "MeasuredDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyProgressSnapshots_MacroProgramWeekId",
                table: "DailyProgressSnapshots",
                column: "MacroProgramWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyProgressSnapshots_UserId_SnapshotDate",
                table: "DailyProgressSnapshots",
                columns: new[] { "UserId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseMuscles_ExerciseId_MuscleGroup_Role",
                table: "ExerciseMuscles",
                columns: new[] { "ExerciseId", "MuscleGroup", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Category",
                table: "Exercises",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Equipment",
                table: "Exercises",
                column: "Equipment");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Name",
                table: "Exercises",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_PrimaryMuscle",
                table: "Exercises",
                column: "PrimaryMuscle");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSets_WorkoutSessionExerciseId_SetNumber",
                table: "ExerciseSets",
                columns: new[] { "WorkoutSessionExerciseId", "SetNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureSnapshots_UserId_SnapshotDate",
                table: "ExpenditureSnapshots",
                columns: new[] { "UserId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_Barcode",
                table: "FoodItems",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_Category",
                table: "FoodItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_CreatedByUserId",
                table: "FoodItems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_Name",
                table: "FoodItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntries_FoodItemId",
                table: "FoodLogEntries",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntries_FoodLogId_MealType",
                table: "FoodLogEntries",
                columns: new[] { "FoodLogId", "MealType" });

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntries_FoodServingId",
                table: "FoodLogEntries",
                column: "FoodServingId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntries_RecipeId",
                table: "FoodLogEntries",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogEntryMicros_FoodLogEntryId",
                table: "FoodLogEntryMicros",
                column: "FoodLogEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogs_UserId_LogDate",
                table: "FoodLogs",
                columns: new[] { "UserId", "LogDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodServings_FoodItemId_IsDefault",
                table: "FoodServings",
                columns: new[] { "FoodItemId", "IsDefault" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthIntegrations_UserId_Provider",
                table: "HealthIntegrations",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoggingStreaks_UserId_StreakType",
                table: "LoggingStreaks",
                columns: new[] { "UserId", "StreakType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MacroProgramDayTargets_MacroProgramId_DayOfWeek",
                table: "MacroProgramDayTargets",
                columns: new[] { "MacroProgramId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MacroPrograms_UserId",
                table: "MacroPrograms",
                column: "UserId",
                unique: true,
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MacroProgramWeeks_MacroProgramId_EffectiveFrom",
                table: "MacroProgramWeeks",
                columns: new[] { "MacroProgramId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodLogs_UserId_StartDate",
                table: "PeriodLogs",
                columns: new[] { "UserId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_FoodItemId",
                table: "RecipeIngredients",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_FoodServingId",
                table: "RecipeIngredients",
                column: "FoodServingId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId_SortOrder",
                table: "RecipeIngredients",
                columns: new[] { "RecipeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CreatedById",
                table: "Recipes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CreatedByUserId",
                table: "Recipes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_IsPublic",
                table: "Recipes",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderSettings_UserId_Type",
                table: "ReminderSettings",
                columns: new[] { "UserId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_ExternalSubscriptionId",
                table: "UserSubscriptions",
                column: "ExternalSubscriptionId",
                filter: "[ExternalSubscriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionPlanId",
                table: "UserSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_Status",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WaterLogs_FoodLogId",
                table: "WaterLogs",
                column: "FoodLogId");

            migrationBuilder.CreateIndex(
                name: "IX_WaterLogs_UserId_LoggedAt",
                table: "WaterLogs",
                columns: new[] { "UserId", "LoggedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyCheckIns_MacroProgramWeekId",
                table: "WeeklyCheckIns",
                column: "MacroProgramWeekId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyCheckIns_UserId_CheckInDate",
                table: "WeeklyCheckIns",
                columns: new[] { "UserId", "CheckInDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightEntries_UserId_LogDate",
                table: "WeightEntries",
                columns: new[] { "UserId", "LogDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionExercises_ExerciseId",
                table: "WorkoutSessionExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessionExercises_WorkoutSessionId_SortOrder",
                table: "WorkoutSessionExercises",
                columns: new[] { "WorkoutSessionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId_SessionDate",
                table: "WorkoutSessions",
                columns: new[] { "UserId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId_Status",
                table: "WorkoutSessions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_WorkoutTemplateId",
                table: "WorkoutSessions",
                column: "WorkoutTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercises_ExerciseId",
                table: "WorkoutTemplateExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplateExercises_WorkoutTemplateId_SortOrder",
                table: "WorkoutTemplateExercises",
                columns: new[] { "WorkoutTemplateId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplates_IsPublic",
                table: "WorkoutTemplates",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplates_IsSystemTemplate",
                table: "WorkoutTemplates",
                column: "IsSystemTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplates_WorkoutGoal",
                table: "WorkoutTemplates",
                column: "WorkoutGoal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BodyMetrics");

            migrationBuilder.DropTable(
                name: "DailyProgressSnapshots");

            migrationBuilder.DropTable(
                name: "ExerciseMuscles");

            migrationBuilder.DropTable(
                name: "ExerciseSets");

            migrationBuilder.DropTable(
                name: "ExpenditureSnapshots");

            migrationBuilder.DropTable(
                name: "FoodLogEntryMicros");

            migrationBuilder.DropTable(
                name: "HealthIntegrations");

            migrationBuilder.DropTable(
                name: "LoggingStreaks");

            migrationBuilder.DropTable(
                name: "MacroProgramDayTargets");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PeriodLogs");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "ReminderSettings");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "WaterLogs");

            migrationBuilder.DropTable(
                name: "WeeklyCheckIns");

            migrationBuilder.DropTable(
                name: "WeightEntries");

            migrationBuilder.DropTable(
                name: "WorkoutTemplateExercises");

            migrationBuilder.DropTable(
                name: "WorkoutSessionExercises");

            migrationBuilder.DropTable(
                name: "FoodLogEntries");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "MacroProgramWeeks");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "WorkoutSessions");

            migrationBuilder.DropTable(
                name: "FoodLogs");

            migrationBuilder.DropTable(
                name: "FoodServings");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "MacroPrograms");

            migrationBuilder.DropTable(
                name: "WorkoutTemplates");

            migrationBuilder.DropTable(
                name: "FoodItems");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
