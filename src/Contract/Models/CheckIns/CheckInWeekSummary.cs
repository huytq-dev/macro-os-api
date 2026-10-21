namespace Contract;

/// <param name="RateActualKgPerWeek">Δtrend 7 ngày (dương = giảm, cùng chiều rate mục tiêu).</param>
public sealed record CheckInWeekSummary(
    decimal AvgCaloriesIn,
    int DaysLogged,
    int DaysMarkedIncomplete,
    decimal? TrendStartKg,
    decimal? TrendNowKg,
    decimal? RateActualKgPerWeek,
    decimal RateTargetKgPerWeek,
    decimal TdeeKcal,
    string ExpenditureStatus
);
