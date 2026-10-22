namespace Contract;

/// <param name="IsLogged">Ngày có FoodLog; false = chưa log (UI hiện –/N/A), khác với log 0 kcal.</param>
/// <param name="EstimatedTdeeKcal">TDEE ước lượng cùng ngày (Energy Balance chế độ Expenditure); null nếu chưa có snapshot.</param>
public sealed record DailyProgressModel(
    DateOnly SnapshotDate,
    bool IsLogged,
    decimal CaloriesConsumed,
    decimal CaloriesTarget,
    decimal ProteinConsumed,
    decimal CarbsConsumed,
    decimal FatConsumed,
    decimal? TrendWeightKg,
    Guid? MacroProgramWeekId,
    decimal? EstimatedTdeeKcal,
    string? TargetProvenance = null
);
// TargetProvenance (CaloriesTarget): VERIFIED | LEGACY_UNVERIFIED | null (chưa có program phủ ngày đó). LEGACY_UNVERIFIED: chỉ tham khảo, FE KHÔNG dùng để tính tỉ lệ đạt/streak/badge.
