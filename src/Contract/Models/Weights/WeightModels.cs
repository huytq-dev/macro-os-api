namespace Contract;

public sealed record WeightEntryModel(
    Guid Id,
    DateOnly LogDate,
    decimal WeightKg,
    decimal? TrendWeightKg,
    string? Notes
);

/// <param name="RateKgPerWeek">Tốc độ thay đổi trend 7 ngày gần nhất (kg/tuần), âm = giảm.</param>
public sealed record WeightEntriesResponse(
    IReadOnlyList<WeightEntryModel> Items,
    decimal? LatestWeightKg,
    decimal? LatestTrendKg,
    decimal? RateKgPerWeek
);
