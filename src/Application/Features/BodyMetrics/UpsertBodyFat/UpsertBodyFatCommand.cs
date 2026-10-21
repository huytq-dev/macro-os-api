namespace Application;

/// <summary>UC-E04: ghi % mỡ (MeasuredDate null = hôm nay theo múi giờ user, không được ở tương lai); cùng ngày + cùng phương pháp thì ghi đè.</summary>
public sealed record UpsertBodyFatCommand(DateOnly? MeasuredDate, MeasurementMethod Method, decimal BodyFatPercent, string? Notes) : IRequest<Guid>;
