namespace Application;

/// <param name="KcalIn">Tổng kcal ngày; null = chưa log.</param>
/// <param name="TrendKg">Trend weight ngày có cân; null = không cân.</param>
public sealed record ExpenditureDay(DateOnly Date, decimal? KcalIn, decimal? TrendKg);
