using System.Text.Json;

namespace Application;

/// <summary>
/// Snapshot GỐC của entry tại lúc log, lưu trong cột <c>FoodLogEntry.ServingSnapshotJson</c> (cột đã có từ S8, không đổi schema): khẩu phần và gram của khẩu phần LÚC LOG, lượng gốc (gram với món, số khẩu phần với recipe)
/// và macro gốc. Sửa số lượng/khẩu phần luôn co giãn từ macro gốc một lần (làm tròn một lần) nên sửa đi sửa lại không trôi số; không bao giờ đọc lại FoodItem/Recipe hiện tại.
/// Entry cũ (trước khi có snapshot này) có JSON null: xem <see cref="EntryEdit"/>.
/// </summary>
public sealed record EntryServingSnapshot(
    int V, Guid? ServingId, string? ServingName, decimal? ServingGrams, decimal BaseAmount,
    decimal Kcal, decimal Protein, decimal Carbs, decimal Fat, decimal? Fiber, decimal? Sodium)
{
    public const int Version = 1;
    private const int MaxNameLength = 100;

    /// <summary>null khi chưa có (entry cũ), rỗng, sai định dạng hoặc phiên bản lạ: coi như không có snapshot gốc, không đoán.</summary>
    public static EntryServingSnapshot? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<EntryServingSnapshot>(json) is { V: Version } s ? s : null; }
        catch (JsonException) { return null; }
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    /// <summary>Gắn snapshot gốc vào entry vừa dựng (macro cột đã được ghi): <paramref name="baseAmount"/> = gram (món) hoặc số khẩu phần (recipe).</summary>
    public static void Stamp(FoodLogEntry e, Guid? servingId, string? servingName, decimal? servingGrams, decimal baseAmount)
        => e.ServingSnapshotJson = new EntryServingSnapshot(Version, servingId, servingName is { Length: > MaxNameLength } n ? n[..MaxNameLength] : servingName, servingGrams, baseAmount,
            e.Calories, e.Protein, e.Carbs, e.Fat, e.Fiber, e.Sodium).ToJson();
}
