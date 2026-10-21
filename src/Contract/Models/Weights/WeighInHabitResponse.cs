namespace Contract;

/// <summary>
/// Dữ liệu trang Habits (Weigh-In). <c>Days</c> chỉ có các ngày có dữ liệu của kỳ đã chọn (cả năm cho heatmap, một tháng cho lịch); các số còn lại là toàn thời gian.
/// </summary>
/// <param name="Month">null = cả năm.</param>
/// <param name="WeighInDaysInPeriod">Số ngày có cân trong kỳ (không tính ngày chỉ có % mỡ).</param>
/// <param name="TotalWeighIns">Tổng số ngày đã cân từ trước tới nay.</param>
/// <param name="CurrentStreak">Số ngày cân liên tiếp kết thúc hôm nay; hôm nay chưa cân thì tính đến hôm qua (0 nếu cả hôm nay lẫn hôm qua đều trống).</param>
/// <param name="LongestStreak">Chuỗi liên tiếp dài nhất từ trước tới nay.</param>
/// <param name="StreakIncludesToday">true = hôm nay đã cân; false và CurrentStreak &gt; 0 = chuỗi sẽ đứt nếu hôm nay không cân (FE nhắc "cân hôm nay để giữ streak").</param>
/// <param name="Today">Hôm nay theo múi giờ của user (FE dùng để tô ngày hôm nay, không tự đoán theo giờ máy).</param>
public sealed record WeighInHabitResponse(
    int Year,
    int? Month,
    IReadOnlyList<WeighInDayModel> Days,
    int WeighInDaysInPeriod,
    int TotalWeighIns,
    int CurrentStreak,
    int LongestStreak,
    bool StreakIncludesToday,
    DateOnly? FirstWeighInDate,
    DateOnly? LastWeighInDate,
    DateOnly Today
);
