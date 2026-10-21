namespace Application;

/// <summary>
/// Lưu MỘT PHẦN của một ngày trong một lần (một transaction): chỉ các ô được gửi (không null) bị ghi/ghi đè, các ô còn lại GIỮ NGUYÊN;
/// <paramref name="Clear"/> liệt kê khoá chỉ số cần xoá (số đo vòng như "waist", "leftBicep" hoặc "visualBodyFat"). Khác PUT full-body (thay cả ngày).
/// Date null = hôm nay theo múi giờ user, không ở tương lai. Một chỉ số một giá trị mỗi ngày (ghi lại là ghi đè).
/// </summary>
public sealed record PatchFullBodyCommand(DateOnly? Date, decimal? VisualBodyFatPercent, TapeMeasurementsModel? Tape, List<string>? Clear) : IRequest<FullBodyResponse>
{
    /// <summary>khoá → giá trị cần ghi, hoặc null = xoá.</summary>
    public Dictionary<string, decimal?> Changes()
    {
        var changes = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        if (VisualBodyFatPercent is { } percent) changes[BodyMeasurements.VisualBodyFatKey] = percent;
        if (Tape is { } tape)
            foreach (var m in BodyMeasurements.All)
                if (m.FromModel(tape) is { } cm) changes[m.Key] = cm;
        foreach (var key in Clear ?? []) changes.TryAdd(BodyMeasurements.Find(key)?.Key ?? key, null);
        return changes;
    }
}
