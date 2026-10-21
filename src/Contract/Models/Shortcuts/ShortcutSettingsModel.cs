namespace Contract;

/// <param name="ToolbarStyle">SearchAndBarcode | SearchBarcodeCustom | FourCustom | None.</param>
/// <param name="Items">Đủ catalog, theo thứ tự user chọn; "Top Shortcut" = 4 dòng ĐANG BẬT đầu tiên.</param>
/// <param name="IsDefault">true khi user chưa lưu (đang trả mặc định của BE).</param>
public sealed record ShortcutSettingsModel(string ToolbarStyle, IReadOnlyList<ShortcutItemModel> Items, bool IsDefault);
