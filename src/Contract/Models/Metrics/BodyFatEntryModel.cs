namespace Contract;

// --- Body fat ---

public sealed record BodyFatEntryModel(Guid Id, DateOnly MeasuredDate, string Method, decimal BodyFatPercent, string? Notes);
