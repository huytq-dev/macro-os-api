namespace Contract;

// --- Period ---

public sealed record PeriodLogModel(
    Guid Id,
    DateOnly StartDate,
    DateOnly? EndDate,
    int? CycleLengthDays,
    int? PeriodLengthDays,
    string? Symptoms,
    string? Notes
);
