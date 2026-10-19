namespace Contract;

/// <param name="Key">Khoá cố định của BE (search, barcode, ai, weight, recipes, quick-add, photos, metrics, your-foods, new-recipe, new-food, favorites); nhãn/icon do FE giữ.</param>
/// <param name="Color">red | orange | yellow | green | teal | blue | purple, hoặc null = không màu.</param>
public sealed record ShortcutItemModel(string Key, bool Enabled, string? Color);

/// <param name="ToolbarStyle">SearchAndBarcode | SearchBarcodeCustom | FourCustom | None.</param>
/// <param name="Items">Đủ catalog, theo thứ tự user chọn; "Top Shortcut" = 4 dòng ĐANG BẬT đầu tiên.</param>
/// <param name="IsDefault">true khi user chưa lưu (đang trả mặc định của BE).</param>
public sealed record ShortcutSettingsModel(string ToolbarStyle, IReadOnlyList<ShortcutItemModel> Items, bool IsDefault);
