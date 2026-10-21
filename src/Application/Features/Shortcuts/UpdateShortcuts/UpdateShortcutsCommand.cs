namespace Application;

/// <summary>
/// Thay toàn bộ cấu hình (idempotent, tạo dòng nếu chưa có). <paramref name="Items"/> theo thứ tự user chọn; khoá còn thiếu được BE bổ sung ở cuối (tắt).
/// Kiểm: khoá thuộc catalog và không trùng, màu thuộc bảng 7 màu hoặc null, ≥ 1 shortcut bật và đủ cho kiểu toolbar (Custom cần shortcut thứ 3, Four Custom cần 4).
/// </summary>
public sealed record UpdateShortcutsCommand(ToolbarStyle ToolbarStyle, List<ShortcutItemModel> Items) : IRequest<ShortcutSettingsModel>;
