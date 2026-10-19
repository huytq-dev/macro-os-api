using System.Text.Json;

namespace Application;

/// <summary>
/// Catalog shortcut của nút "+" ở thanh nav: khoá cố định, thứ tự + trạng thái bật mặc định (theo thiết kế MacroFactor), bảng 7 màu.
/// Nhãn, mô tả, icon là chuỗi UI nên nằm ở FE. Thêm shortcut mới = thêm một dòng ở đây (cấu hình đã lưu tự được bổ sung dòng mới, tắt). Hàm thuần.
/// </summary>
public static class ShortcutCatalog
{
    // Thứ tự = thứ tự mặc định: 4 đầu là "Top Shortcut" (Search, Barcode, AI, Weight)
    private static readonly (string Key, bool Enabled)[] Entries =
    [
        ("search", true), ("barcode", true), ("ai", true), ("weight", true),
        ("recipes", true), ("quick-add", true), ("photos", true), ("metrics", true),
        ("your-foods", false), ("new-recipe", false), ("new-food", false), ("favorites", false),
    ];

    public static readonly IReadOnlyList<string> Colors = ["red", "orange", "yellow", "green", "teal", "blue", "purple"];

    public static IReadOnlyList<string> Keys => Entries.Select(e => e.Key).ToList();

    public static List<ShortcutItemModel> Defaults() => Entries.Select(e => new ShortcutItemModel(e.Key, e.Enabled, null)).ToList();

    /// <summary>Số shortcut đang bật tối thiểu để toolbar kiểu này đủ nút: SearchBarcodeCustom cần shortcut thứ 3, FourCustom cần 4, còn lại cần ≥ 1 (ít nhất một shortcut ở sheet).</summary>
    public static int RequiredEnabled(ToolbarStyle style) => style switch
    {
        ToolbarStyle.SearchBarcodeCustom => 3,
        ToolbarStyle.FourCustom => 4,
        _ => 1,
    };

    /// <summary>Giữ thứ tự user gửi, bổ sung khoá còn thiếu ở cuối (tắt, không màu) để cấu hình luôn đủ catalog; đã được validator kiểm trùng/khoá lạ.</summary>
    public static List<ShortcutItemModel> Normalize(IEnumerable<ShortcutItemModel> items)
    {
        var result = items.Select(i => new ShortcutItemModel(i.Key, i.Enabled, string.IsNullOrWhiteSpace(i.Color) ? null : i.Color)).ToList();
        foreach (var e in Entries.Where(e => result.All(i => i.Key != e.Key)))
            result.Add(new ShortcutItemModel(e.Key, false, null));
        return result;
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Serialize(IEnumerable<ShortcutItemModel> items) => JsonSerializer.Serialize(items, Json);

    /// <summary>Đọc cột JSON; hỏng hoặc rỗng thì trả mặc định (không ném lỗi để GET luôn dùng được).</summary>
    public static List<ShortcutItemModel> Deserialize(string? json)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<ShortcutItemModel>>(json ?? "", Json);
            return items is { Count: > 0 } ? Normalize(items.Where(i => Keys.Contains(i.Key)).DistinctBy(i => i.Key)) : Defaults();
        }
        catch (JsonException)
        {
            return Defaults();
        }
    }
}
