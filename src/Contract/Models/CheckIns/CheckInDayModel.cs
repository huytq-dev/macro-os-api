namespace Contract;

/// <param name="Kcal">null = chưa log ngày đó.</param>
/// <param name="TargetKcal">Target kcal hiệu lực của ngày đó (DayTarget hoặc target tuần); null nếu chưa có target.</param>
/// <param name="SuspectedIncomplete">Đã log nhưng 0 &lt; kcal &lt; tỉ lệ × TDEE của ngày đó.</param>
/// <param name="MarkedIncomplete">User đã đánh dấu; expenditure bỏ qua ngày này.</param>
/// <param name="TargetProvenance">VERIFIED | LEGACY_UNVERIFIED | null (khi TargetKcal null): LEGACY_UNVERIFIED = target chỉ để tham khảo, không dùng cho kết luận đạt/không đạt.</param>
public sealed record CheckInDayModel(DateOnly Date, decimal? Kcal, int? TargetKcal, bool SuspectedIncomplete, bool MarkedIncomplete, string? TargetProvenance = null);
