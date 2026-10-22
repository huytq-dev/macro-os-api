namespace Contract;

/// <summary>
/// Một ngày của chuỗi lịch sử một nutrient (trang chi tiết nutrient): lượng đã ăn + mục tiêu CỦA NGÀY ĐÓ + dải Range.
/// </summary>
/// <param name="Amount">Lượng đã ăn trong ngày. null = chưa log ngày đó hoặc ngày có log nhưng không món nào có dữ liệu chất này (khác 0). Ngày nhịn ăn: 0 cho energy/protein/carbs/fat/fiber/sodium/net carbs, null cho chất khác.</param>
/// <param name="Floor">Cận dưới Range (chỉ vi chất; null với Calories/P/F/C).</param>
/// <param name="Target">Calories/P/F/C: target của program CỦA NGÀY ĐÓ (tuần program hiệu lực tại ngày đó); chất khác: target hiệu lực hiện tại (user ghi đè hoặc mặc định) áp cho mọi ngày. null nếu không có.</param>
/// <param name="Ceiling">Cận trên Range (chỉ vi chất).</param>
/// <param name="IsFasting">Ngày nhịn ăn (tính đã log).</param>
/// <param name="IsIncomplete">Ngày đánh dấu log chưa đủ: vẫn có Amount nhưng KHÔNG được tính vào thống kê và bucket.</param>
public sealed record NutrientSeriesDay(
    DateOnly Date,
    decimal? Amount,
    decimal? Floor,
    decimal? Target,
    decimal? Ceiling,
    bool IsFasting,
    bool IsIncomplete,
    string? TargetProvenance = null
);
// TargetProvenance: chỉ Calories/P/F/C (target từ program): VERIFIED | LEGACY_UNVERIFIED (Target chỉ để tham khảo, "Chưa xác minh"; ngày đó bị loại khỏi AverageTarget của bucket/summary); null với chất khác hoặc chưa có target.
