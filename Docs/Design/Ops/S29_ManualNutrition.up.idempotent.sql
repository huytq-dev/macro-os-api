BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    ALTER TABLE [FoodLogs] ADD [ManualCaloriesKcal] decimal(8,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    ALTER TABLE [FoodLogs] ADD [ManualCarbsG] decimal(8,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    ALTER TABLE [FoodLogs] ADD [ManualFatG] decimal(8,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    ALTER TABLE [FoodLogs] ADD [ManualProteinG] decimal(8,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    EXEC(N'ALTER TABLE [FoodLogs] ADD CONSTRAINT [CK_FoodLogs_ManualNutrition_Range] CHECK (([ManualCaloriesKcal] IS NULL OR ([ManualCaloriesKcal] >= 0 AND [ManualCaloriesKcal] <= 10000)) AND ([ManualProteinG] IS NULL OR ([ManualProteinG] >= 0 AND [ManualProteinG] <= 1000)) AND ([ManualCarbsG] IS NULL OR ([ManualCarbsG] >= 0 AND [ManualCarbsG] <= 1000)) AND ([ManualFatG] IS NULL OR ([ManualFatG] >= 0 AND [ManualFatG] <= 1000)))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025103001_S29_ManualNutrition'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025103001_S29_ManualNutrition', N'10.0.3');
END;

COMMIT;
GO

