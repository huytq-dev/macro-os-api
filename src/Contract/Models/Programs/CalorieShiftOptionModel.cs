namespace Contract;

/// <summary>
/// Cách chỉnh để tổ hợp hợp lệ, tính từ cùng công thức dịch kcal (không có hằng số mới); chỉ trả những cách thật sự cho kết quả hợp lệ.
/// REDUCE_PERCENT: <see cref="MaxPercent"/> = % lớn nhất còn hợp lệ với đúng các ngày đã chọn. REDUCE_DAYS: <see cref="MaxDays"/> = số ngày được chọn lớn nhất còn hợp lệ với đúng % đã chọn.
/// LOWER_FLOOR: hạ sàn kcal xuống tối đa <see cref="MaxFloorKcal"/>. REMOVE_FLOOR: tắt sàn kcal. Macro không bao giờ bị tự đổi.
/// </summary>
public sealed record CalorieShiftOptionModel(
    string Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? MaxPercent = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? MaxDays = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? MaxFloorKcal = null);
