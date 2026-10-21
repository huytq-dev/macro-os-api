namespace Contract;

/// <summary>Thống kê khoảng: Average = trung bình MỖI NGÀY ĐÃ LOG (có Amount, không IsIncomplete), không chia cho độ dài khoảng. Không có ngày nào → LoggedDays 0 và các giá trị null.</summary>
public sealed record NutrientSeriesSummary(
    DateOnly From,
    DateOnly To,
    int LoggedDays,
    decimal? Average,
    decimal? Min,
    decimal? Max,
    decimal? AverageTarget
);
