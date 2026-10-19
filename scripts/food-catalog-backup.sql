/*
  BACKUP trước khi chạy `reseed-foods --apply` (xem Docs/Design/Food_Reseed.md).
  Chép nguyên 4 bảng mà lệnh sẽ SỬA/XOÁ dòng vào bảng bak_<Bảng>_<yyyyMMdd_HHmm> trong CÙNG database (SELECT INTO), không cần quyền ghi file/backup server.
    FoodItems      : đổi Name, Language, IconKey
    FoodServings   : đổi ServingName
    FoodAliases    : XOÁ alias tiếng Việt của món catalog
    CanonicalFoods : đổi Name/NameNormalized (8 canonical tên Việt), xoá NameVi
  Chạy bằng SSMS/Azure Data Studio/sqlcmd trên ĐÚNG database mà API đang dùng. Ghi lại hậu tố in ra ở cuối để dùng cho food-catalog-restore.sql.
  Bảng bak_* có thể xoá thủ công khi đã yên tâm.
*/
SET NOCOUNT ON;

DECLARE @s nvarchar(16) = FORMAT(SYSUTCDATETIME(), 'yyyyMMdd_HHmm');
DECLARE @tables TABLE (Name sysname);
INSERT @tables VALUES (N'FoodItems'), (N'FoodServings'), (N'FoodAliases'), (N'CanonicalFoods');

DECLARE @n sysname, @sql nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @tables;
OPEN c;
FETCH NEXT FROM c INTO @n;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'SELECT * INTO dbo.[bak_' + @n + N'_' + @s + N'] FROM dbo.[' + @n + N']';
    EXEC sp_executesql @sql;
    SET @sql = N'SELECT N''' + @n + N''' AS [Table], COUNT(*) AS [Rows backed up] FROM dbo.[bak_' + @n + N'_' + @s + N']';
    EXEC sp_executesql @sql;
    FETCH NEXT FROM c INTO @n;
END
CLOSE c;
DEALLOCATE c;

PRINT N'Backup xong. Hậu tố = ' + @s;
PRINT N'Chạy reseed:  dotnet run --project src/API -- reseed-foods --apply --backed-up';
PRINT N'Hoàn tác:     food-catalog-restore.sql với @s = ''' + @s + N'''';

/* Tham khảo (chỉ đọc): số dòng dữ liệu người dùng đang trỏ vào món catalog (lý do KHÔNG xoá cứng món) */
SELECT 'FoodLogEntries' AS [Table], COUNT(*) AS [Rows referencing catalog foods]
FROM dbo.FoodLogEntries e JOIN dbo.FoodItems f ON f.Id = e.FoodItemId WHERE f.ProviderCode IN ('seed', 'vn-fct', 'cond')
UNION ALL SELECT 'RecipeIngredients', COUNT(*) FROM dbo.RecipeIngredients r JOIN dbo.FoodItems f ON f.Id = r.FoodItemId WHERE f.ProviderCode IN ('seed', 'vn-fct', 'cond')
UNION ALL SELECT 'FoodFavorites', COUNT(*) FROM dbo.FoodFavorites r JOIN dbo.FoodItems f ON f.Id = r.FoodItemId WHERE f.ProviderCode IN ('seed', 'vn-fct', 'cond')
UNION ALL SELECT 'UserFoodIcons', COUNT(*) FROM dbo.UserFoodIcons r JOIN dbo.FoodItems f ON f.Id = r.FoodItemId WHERE f.ProviderCode IN ('seed', 'vn-fct', 'cond')
UNION ALL SELECT 'PlannedMeals', COUNT(*) FROM dbo.PlannedMeals r JOIN dbo.FoodItems f ON f.Id = r.FoodItemId WHERE f.ProviderCode IN ('seed', 'vn-fct', 'cond')
UNION ALL SELECT 'MealTemplateItems', COUNT(*) FROM dbo.MealTemplateItems r JOIN dbo.FoodItems f ON f.Id = r.FoodItemId WHERE f.ProviderCode IN ('seed', 'vn-fct', 'cond')
UNION ALL SELECT 'AiIngredientCaches', COUNT(*) FROM dbo.AiIngredientCaches r JOIN dbo.FoodItems f ON f.Id = r.FoodItemId WHERE f.ProviderCode IN ('seed', 'vn-fct', 'cond');
