namespace Application;

/// <summary>UC-E01: cân ngày, 1 entry/ngày; LogDate null = hôm nay. Ghi đè nếu đã có.</summary>
public sealed record UpsertWeightEntryCommand(
    DateOnly? LogDate,
    decimal WeightKg,
    string? Notes
) : IRequest<Guid>;
