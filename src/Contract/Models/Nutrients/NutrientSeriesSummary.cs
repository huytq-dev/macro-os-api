namespace Contract;

/// <summary>Thống kê khoảng: Average = trung bình MỖI NGÀY ĐÃ LOG (có Amount, không IsIncomplete), không chia cho độ dài khoảng. Không có ngày nào → LoggedDays 0 và các giá trị null.</summary>
public sealed record NutrientSeriesSummary(
    DateOnly From,
    DateOnly To,
    int LoggedDays,
    decimal? Average,
    decimal? Min,
    decimal? Max,
    decimal? AverageTarget,
    int UnverifiedTargetDays = 0
);
// AverageTarget chỉ tính ngày có target VERIFIED (hoặc không phụ thuộc program); UnverifiedTargetDays = số ngày hợp lệ bị loại vì target LEGACY_UNVERIFIED.
