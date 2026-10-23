namespace Application;

/// <summary>
/// UC-E01: cân ngày, 1 entry/ngày; LogDate null = hôm nay theo múi giờ user, không được ở tương lai. Ghi đè nếu đã có.
/// BodyFatPercent (tuỳ chọn, 2–70) ghi % mỡ đo bằng cân của cùng ngày (mỗi ngày một dòng); null = giữ nguyên, ClearBodyFat = true (khi BodyFatPercent null) = xoá.
/// CreateOnly (chỉ dùng nội bộ, ví dụ Onboarding): chỉ TẠO khi ngày đó chưa có lần cân; đã có (kể cả do request đồng thời vừa tạo) ⇒ trả Id dòng đó và KHÔNG sửa gì (không ghi đè cân nặng, không xoá ghi chú).
/// </summary>
public sealed record UpsertWeightEntryCommand(
    DateOnly? LogDate,
    decimal WeightKg,
    string? Notes,
    decimal? BodyFatPercent = null,
    bool ClearBodyFat = false,
    [property: System.Text.Json.Serialization.JsonIgnore] bool CreateOnly = false
) : IRequest<Guid>;
