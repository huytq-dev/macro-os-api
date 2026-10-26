namespace Application;

/// <summary>
/// Remove Manual Override: chỉ xoá 4 cột manual của ngày. Món đã log, ghi chú, nước, cờ nhịn ăn giữ nguyên (cờ chưa-đủ chỉ tự bỏ khi ngày thành trống thật, R2-4);
/// hiệu lực quay về Food Log nếu có. 404 nếu user không có ngày đó; không có override ⇒ không làm gì (thành công, idempotent). Trả lại ngày sau khi gỡ.
/// </summary>
public sealed record RemoveManualOverrideCommand(DateOnly Date) : IRequest<NutritionDataDayModel>;
