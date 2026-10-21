namespace Contract;

public sealed record WeightEntryModel(
    Guid Id,
    DateOnly LogDate,
    decimal WeightKg,
    decimal? TrendWeightKg,
    string? Notes,
    decimal? BodyFatPercent = null
);
