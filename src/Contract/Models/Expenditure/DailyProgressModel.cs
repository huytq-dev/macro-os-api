namespace Contract;

/// <param name="IsLogged">Ngày có FoodLog; false = chưa log (UI hiện –/N/A), khác với log 0 kcal.</param>
/// <param name="ProteinConsumed">Effective Daily Nutrition (manual thắng Food Log; nhịn ăn hợp lệ = 0); null = chưa biết (ngày chưa log, hoặc override không nhập trường này): không phải 0 g. Tính khi đọc từ nguồn hiệu lực, không đọc cột snapshot.</param>
/// <param name="EstimatedTdeeKcal">TDEE ước lượng cùng ngày (Energy Balance chế độ Expenditure); null nếu chưa có snapshot.</param>
public sealed record DailyProgressModel(
    DateOnly SnapshotDate,
    bool IsLogged,
    decimal CaloriesConsumed,
    decimal CaloriesTarget,
    decimal? ProteinConsumed,
    decimal? CarbsConsumed,
    decimal? FatConsumed,
    decimal? TrendWeightKg,
    Guid? MacroProgramWeekId,
    decimal? EstimatedTdeeKcal,
    string? TargetProvenance = null
);
// TargetProvenance (CaloriesTarget): VERIFIED | LEGACY_UNVERIFIED | null (chưa có program phủ ngày đó). LEGACY_UNVERIFIED: chỉ tham khảo, FE KHÔNG dùng để tính tỉ lệ đạt/streak/badge.
