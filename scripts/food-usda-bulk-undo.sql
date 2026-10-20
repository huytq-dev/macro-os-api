/*
  HOÀN TÁC `import-usda-bulk --apply`: xoá các món USDA do lệnh nhập (ProviderCode = 'usda') mà CHƯA có dữ liệu người dùng trỏ vào.
  Lệnh chỉ THÊM dòng (không sửa/xoá món catalog) nên không cần restore từ backup; script này chỉ gỡ phần đã thêm.
  Đặt @since = thời điểm TRƯỚC khi chạy lệnh (UTC, xem FoodImportRuns.StartedAt của dòng Provider = 'usda-bulk'); món nhập bởi API ImportUsda trước @since không bị đụng.
  Mặc định @dryrun = 1 chỉ ĐẾM; đặt 0 để xoá (một transaction). Món đã được log/yêu thích/dùng trong recipe/kế hoạch/icon/đệm AI thì GIỮ LẠI (FK Restrict).
*/
SET XACT_ABORT ON;
DECLARE @since datetime2 = '2000-01-01';   -- <-- sửa
DECLARE @dryrun bit = 1;

SELECT f.Id INTO #candidates
FROM dbo.FoodItems f
WHERE f.ProviderCode = 'usda' AND f.SourceCapturedAt >= @since
  AND NOT EXISTS (SELECT 1 FROM dbo.FoodLogEntries x WHERE x.FoodItemId = f.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.RecipeIngredients x WHERE x.FoodItemId = f.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.FoodFavorites x WHERE x.FoodItemId = f.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.PlannedMeals x WHERE x.FoodItemId = f.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.MealTemplateItems x WHERE x.FoodItemId = f.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.UserFoodIcons x WHERE x.FoodItemId = f.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.AiIngredientCaches x WHERE x.FoodItemId = f.Id);

SELECT
    (SELECT COUNT(*) FROM dbo.FoodItems WHERE ProviderCode = 'usda' AND SourceCapturedAt >= @since) AS [USDA foods since @since],
    (SELECT COUNT(*) FROM #candidates) AS [Deletable (no user data)],
    (SELECT COUNT(*) FROM dbo.FoodNutrients n JOIN #candidates c ON c.Id = n.FoodItemId) AS [FoodNutrients],
    (SELECT COUNT(*) FROM dbo.FoodServings s JOIN #candidates c ON c.Id = s.FoodItemId) AS [FoodServings],
    (SELECT COUNT(*) FROM dbo.FoodAliases a JOIN #candidates c ON c.Id = a.FoodItemId) AS [FoodAliases];

IF @dryrun = 1 BEGIN PRINT N'DRY-RUN: chưa xoá gì. Đặt @dryrun = 0 để xoá.'; RETURN; END

BEGIN TRAN;
DELETE n FROM dbo.FoodNutrients n JOIN #candidates c ON c.Id = n.FoodItemId;
DELETE s FROM dbo.FoodServings s JOIN #candidates c ON c.Id = s.FoodItemId;
DELETE a FROM dbo.FoodAliases a JOIN #candidates c ON c.Id = a.FoodItemId;
DELETE r FROM dbo.FoodSourceRecords r JOIN #candidates c ON c.Id = r.FoodItemId;
DELETE f FROM dbo.FoodItems f JOIN #candidates c ON c.Id = f.Id;
DELETE FROM dbo.FoodImportRuns WHERE Provider = 'usda-bulk' AND StartedAt >= @since;
COMMIT;
PRINT N'Đã xoá các món USDA nhập hàng loạt chưa được dùng.';
