namespace Contract;

/// <param name="DaysSinceLastStart">Số ngày từ StartDate kỳ gần nhất tới hôm nay; null nếu chưa có kỳ nào.</param>
/// <param name="Days">Các ngày ĐÃ THEO DÕI (đang kinh) trong [from, to] đã trải từ các kỳ (kể cả kỳ bắt đầu trước from, kỳ đang diễn ra tính tới hôm nay), tăng dần: FE vẽ lịch tháng và danh sách "Period · Wed, Oct 21" không phải tự trải khoảng.</param>
/// <param name="LastPeriodLengthDays">Số ngày của kỳ gần nhất (gồm hai đầu; đang diễn ra tính tới hôm nay) cho "Last Period Duration"; null nếu chưa có kỳ nào.</param>
public sealed record PeriodLogsResponse(IReadOnlyList<PeriodLogModel> Items, DateOnly? LastStartDate, int? DaysSinceLastStart, IReadOnlyList<DateOnly> Days, int? LastPeriodLengthDays);
