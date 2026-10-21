namespace Application;

/// <summary>UC-E05: log kỳ kinh. EndDate null = đang trong kỳ. Chỉ để giải thích dao động cân, không ảnh hưởng target.</summary>
public sealed record CreatePeriodLogCommand(DateOnly StartDate, DateOnly? EndDate, string? Symptoms, string? Notes) : IRequest<Guid>;
