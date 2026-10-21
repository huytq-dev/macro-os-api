namespace Contract;

public sealed record BodyFatResponse(IReadOnlyList<BodyFatEntryModel> Items, decimal? LatestPercent);
