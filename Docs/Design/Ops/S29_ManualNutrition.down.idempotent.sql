BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    IF EXISTS (SELECT 1 FROM [FoodLogs] WHERE [ManualCaloriesKcal] IS NOT NULL OR [ManualProteinG] IS NOT NULL OR [ManualCarbsG] IS NOT NULL OR [ManualFatG] IS NOT NULL) THROW 51029, N'S29 Down refused: FoodLogs still holds Manual Nutrition values. Export Id, UserId, LogDate and the four Manual columns to a backup first, then clear them on purpose (UPDATE FoodLogs SET ManualCaloriesKcal = NULL, ManualProteinG = NULL, ManualCarbsG = NULL, ManualFatG = NULL) and run Down again.', 1;
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    ALTER TABLE [FoodLogs] DROP CONSTRAINT [CK_FoodLogs_ManualNutrition_Range];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FoodLogs]') AND [c].[name] = N'ManualCaloriesKcal');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [FoodLogs] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [FoodLogs] DROP COLUMN [ManualCaloriesKcal];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FoodLogs]') AND [c].[name] = N'ManualCarbsG');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [FoodLogs] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [FoodLogs] DROP COLUMN [ManualCarbsG];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FoodLogs]') AND [c].[name] = N'ManualFatG');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [FoodLogs] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [FoodLogs] DROP COLUMN [ManualFatG];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FoodLogs]') AND [c].[name] = N'ManualProteinG');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [FoodLogs] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [FoodLogs] DROP COLUMN [ManualProteinG];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition';
END;

COMMIT;
GO

