namespace Contract;

// --- Steps ---

public sealed record StepEntryModel(Guid Id, DateOnly LogDate, int Steps, string Source);
