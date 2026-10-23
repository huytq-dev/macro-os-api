namespace Contract;

/// <param name="DaysSinceLastStart">Số ngày từ StartDate kỳ gần nhất tới hôm nay; null nếu chưa có kỳ nào.</param>
/// <param name="Days">Các ngày ĐÃ THEO DÕI (đang kinh) trong [from, to] đã trải từ các kỳ (kể cả kỳ bắt đầu trước from, kỳ đang diễn ra tính tới hôm nay), tăng dần: FE vẽ lịch tháng và danh sách "Period · Wed, Oct 21" không phải tự trải khoảng.</param>
/// <param name="LastPeriodLengthDays">Số ngày của kỳ gần nhất (gồm hai đầu; đang diễn ra tính tới hôm nay) cho "Last Period Duration"; null nếu chưa có kỳ nào.</param>
/// <param name="LastPeriodDay">Ngày GẦN NHẤT thực sự được ghi nhận đang kinh (≤ hôm nay local) trên TOÀN BỘ lịch sử (không giới hạn khoảng from/to, không dự đoán); kỳ đang diễn ra (EndDate null) tính hôm nay là ngày đã ghi nhận. null nếu chưa có kỳ hợp lệ.</param>
/// <param name="DaysSinceLastPeriodDay">Hôm nay − <c>LastPeriodDay</c> theo ngày lịch của user (múi giờ hồ sơ); nguồn của "N days ago" trên Dashboard (policy được duyệt, chưa khẳng định là hành vi MacroFactor). null nếu chưa có kỳ. <c>DaysSinceLastStart</c> giữ nguyên (khoảng cách tới ngày BẮT ĐẦU kỳ).</param>
public sealed record PeriodLogsResponse(IReadOnlyList<PeriodLogModel> Items, DateOnly? LastStartDate, int? DaysSinceLastStart, IReadOnlyList<DateOnly> Days, int? LastPeriodLengthDays,
    DateOnly? LastPeriodDay = null, int? DaysSinceLastPeriodDay = null);
