/*
  HOÀN TÁC `reseed-foods --apply` từ bảng bak_* do food-catalog-backup.sql tạo.
  Đặt @s = hậu tố backup (vd N'20261019_1530'). Khôi phục GIÁ TRỊ CŨ của các dòng đã có:
    FoodItems (Name, Language, IconKey), FoodServings (ServingName), CanonicalFoods (Name, NameNormalized, NameVi) và chèn lại alias đã bị xoá.
  KHÔNG xoá dòng mà lệnh đã THÊM (món/alias/canonical mới do seeder bổ sung): chúng vô hại. Chạy trong một transaction.
  Lưu ý: nếu API khởi động lại sau khi hoàn tác mà Seed:IncludeVietnamese vẫn false thì seeder KHÔNG thêm lại alias Việt (đặt = true nếu muốn giữ bản cũ).
*/
SET XACT_ABORT ON;
DECLARE @s nvarchar(16) = N'REPLACE_ME';   -- <-- sửa

IF OBJECT_ID(N'dbo.[bak_FoodItems_' + @s + N']') IS NULL
BEGIN
    RAISERROR(N'Không tìm thấy bảng backup với hậu tố này.', 16, 1);
    RETURN;
END

DECLARE @sql nvarchar(max) = N'
BEGIN TRAN;
UPDATE f SET f.Name = b.Name, f.Language = b.Language, f.IconKey = b.IconKey
FROM dbo.FoodItems f JOIN dbo.[bak_FoodItems_' + @s + N'] b ON b.Id = f.Id;

UPDATE f SET f.ServingName = b.ServingName
FROM dbo.FoodServings f JOIN dbo.[bak_FoodServings_' + @s + N'] b ON b.Id = f.Id;

UPDATE f SET f.Name = b.Name, f.NameNormalized = b.NameNormalized, f.NameVi = b.NameVi
FROM dbo.CanonicalFoods f JOIN dbo.[bak_CanonicalFoods_' + @s + N'] b ON b.Id = f.Id;

INSERT INTO dbo.FoodAliases
SELECT b.* FROM dbo.[bak_FoodAliases_' + @s + N'] b WHERE NOT EXISTS (SELECT 1 FROM dbo.FoodAliases a WHERE a.Id = b.Id);
COMMIT;';
EXEC sp_executesql @sql;
PRINT N'Đã hoàn tác từ backup ' + @s;
