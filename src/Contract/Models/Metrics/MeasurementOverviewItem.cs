namespace Contract;

public sealed record MeasurementOverviewItem(string Key, string Group, DateOnly LatestDate, decimal LatestCm, IReadOnlyList<MeasurementEntryModel> Entries);
