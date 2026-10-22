namespace Contract;

/// <summary>Bucket Day/Week/Month: trung bình các ngày đã log hợp lệ trong bucket (cùng quy tắc với <see cref="EnergyBalanceSummary"/>); chỉ có bucket chứa ít nhất một ngày đã log hợp lệ. AvgExpenditureKcal/AvgVsExpenditureKcal/AvgCaloriesVsExpenditureKcal cùng một tập ngày (có cả kcal và expenditure); tương tự cho target.</summary>
public sealed record EnergyBalanceBucket(
    DateOnly Start,
    DateOnly End,
    int LoggedDays,
    decimal AvgCaloriesKcal,
    decimal? AvgExpenditureKcal,
    decimal? AvgTargetKcal,
    decimal? AvgVsExpenditureKcal,
    decimal? AvgVsTargetKcal,
    decimal? AvgCaloriesVsExpenditureKcal = null,
    decimal? AvgCaloriesVsTargetKcal = null,
    int UnverifiedTargetDays = 0
);
