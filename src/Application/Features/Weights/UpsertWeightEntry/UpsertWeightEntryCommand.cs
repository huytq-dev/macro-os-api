namespace Application;

/// <summary>
/// UC-E01: cân ngày, 1 entry/ngày; LogDate null = hôm nay theo múi giờ user, không được ở tương lai. Ghi đè nếu đã có.
/// BodyFatPercent (tuỳ chọn, 2–70) ghi % mỡ đo bằng cân của cùng ngày (mỗi ngày một dòng); null = giữ nguyên, ClearBodyFat = true (khi BodyFatPercent null) = xoá.
/// </summary>
public sealed record UpsertWeightEntryCommand(
    DateOnly? LogDate,
    decimal WeightKg,
    string? Notes,
    decimal? BodyFatPercent = null,
    bool ClearBodyFat = false
) : IRequest<Guid>;
