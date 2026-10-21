namespace Application;

/// <summary>UC-E07: ghi số bước một ngày (LogDate null = hôm nay); đã có thì ghi đè. Chỉ hiển thị, không vào expenditure.</summary>
public sealed record UpsertStepEntryCommand(DateOnly? LogDate, int Steps) : IRequest<Guid>;
