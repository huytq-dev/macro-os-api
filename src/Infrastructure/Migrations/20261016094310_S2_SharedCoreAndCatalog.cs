using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S2_SharedCoreAndCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataImportCursors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Cursor = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SyncStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataImportCursors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataImportCursors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsLoadable = table.Column<bool>(type: "bit", nullable: false),
                    DefaultIncrementKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AliasNormalized = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseAliases_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AliasNormalized = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodAliases_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodFavorites_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FoodFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    LicenseMetadata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodImages_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetWeightKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    RatePerWeekKg = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Goals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GymProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResponseStatus = table.Column<int>(type: "int", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutrientDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameVi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Group = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    DisplayPrecision = table.Column<byte>(type: "tinyint", nullable: false),
                    ReferenceMin = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    ReferenceMax = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutrientDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgressPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Angle = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsentGivenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressPhotos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseEquipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Requirement = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseEquipment_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseEquipment_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GymEquipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailableCount = table.Column<int>(type: "int", nullable: true),
                    IncrementKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MinLoadKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    MaxLoadKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymEquipment", x => x.Id);
                    table.CheckConstraint("CK_GymEquipment_LoadsNonNegative", "([IncrementKg] IS NULL OR [IncrementKg] > 0) AND ([MinLoadKg] IS NULL OR [MinLoadKg] >= 0) AND ([MaxLoadKg] IS NULL OR [MaxLoadKg] >= 0)");
                    table.ForeignKey(
                        name: "FK_GymEquipment_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymEquipment_GymProfiles_GymProfileId",
                        column: x => x.GymProfileId,
                        principalTable: "GymProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GymExerciseOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rule = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymExerciseOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymExerciseOverrides_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GymExerciseOverrides_GymProfiles_GymProfileId",
                        column: x => x.GymProfileId,
                        principalTable: "GymProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodNutrients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoodItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NutrientDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountPer100g = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: true),
                    AmountStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodNutrients", x => x.Id);
                    table.CheckConstraint("CK_FoodNutrients_AmountNonNegative", "[AmountPer100g] IS NULL OR [AmountPer100g] >= 0");
                    table.ForeignKey(
                        name: "FK_FoodNutrients_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FoodNutrients_NutrientDefinitions_NutrientDefinitionId",
                        column: x => x.NutrientDefinitionId,
                        principalTable: "NutrientDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataImportCursors_UserId_Provider_ResourceType",
                table: "DataImportCursors",
                columns: new[] { "UserId", "Provider", "ResourceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Code",
                table: "Equipment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAliases_ExerciseId",
                table: "ExerciseAliases",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAliases_Locale_AliasNormalized",
                table: "ExerciseAliases",
                columns: new[] { "Locale", "AliasNormalized" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEquipment_EquipmentId",
                table: "ExerciseEquipment",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEquipment_ExerciseId_EquipmentId",
                table: "ExerciseEquipment",
                columns: new[] { "ExerciseId", "EquipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodAliases_FoodItemId",
                table: "FoodAliases",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodAliases_Locale_AliasNormalized",
                table: "FoodAliases",
                columns: new[] { "Locale", "AliasNormalized" });

            migrationBuilder.CreateIndex(
                name: "IX_FoodFavorites_FoodItemId",
                table: "FoodFavorites",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodFavorites_UserId_FoodItemId",
                table: "FoodFavorites",
                columns: new[] { "UserId", "FoodItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodImages_FoodItemId",
                table: "FoodImages",
                column: "FoodItemId",
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FoodNutrients_FoodItemId_NutrientDefinitionId",
                table: "FoodNutrients",
                columns: new[] { "FoodItemId", "NutrientDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodNutrients_NutrientDefinitionId",
                table: "FoodNutrients",
                column: "NutrientDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_UserId_Status",
                table: "Goals",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GymEquipment_EquipmentId",
                table: "GymEquipment",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GymEquipment_GymProfileId_EquipmentId",
                table: "GymEquipment",
                columns: new[] { "GymProfileId", "EquipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymExerciseOverrides_ExerciseId",
                table: "GymExerciseOverrides",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_GymExerciseOverrides_GymProfileId_ExerciseId",
                table: "GymExerciseOverrides",
                columns: new[] { "GymProfileId", "ExerciseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymProfiles_UserId",
                table: "GymProfiles",
                column: "UserId",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_ExpiresAt",
                table: "IdempotencyRecords",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_UserId_Key_Endpoint",
                table: "IdempotencyRecords",
                columns: new[] { "UserId", "Key", "Endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NutrientDefinitions_Code",
                table: "NutrientDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgressPhotos_UserId_CapturedAt",
                table: "ProgressPhotos",
                columns: new[] { "UserId", "CapturedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataImportCursors");

            migrationBuilder.DropTable(
                name: "ExerciseAliases");

            migrationBuilder.DropTable(
                name: "ExerciseEquipment");

            migrationBuilder.DropTable(
                name: "FoodAliases");

            migrationBuilder.DropTable(
                name: "FoodFavorites");

            migrationBuilder.DropTable(
                name: "FoodImages");

            migrationBuilder.DropTable(
                name: "FoodNutrients");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "GymEquipment");

            migrationBuilder.DropTable(
                name: "GymExerciseOverrides");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropTable(
                name: "ProgressPhotos");

            migrationBuilder.DropTable(
                name: "NutrientDefinitions");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "GymProfiles");
        }
    }
}
