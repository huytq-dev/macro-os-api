namespace Application;

/// <summary>Một kỳ kinh (khoảng ngày liên tục). End null = đang trong kỳ (tính tới hôm nay).</summary>
public sealed record PeriodRange(Guid Id, DateOnly Start, DateOnly? End, string? Symptoms, string? Notes);
