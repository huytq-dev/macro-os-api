namespace Contract;

/// <summary>Bucket Day/Week/Month: trung bình các ngày đã log hợp lệ trong bucket (cùng quy tắc với <see cref="EnergyBalanceSummary"/>); chỉ có bucket chứa ít nhất một ngày đã log hợp lệ.</summary>
public sealed record EnergyBalanceBucket(
    DateOnly Start,
    DateOnly End,
    int LoggedDays,
    decimal AvgCaloriesKcal,
    decimal? AvgExpenditureKcal,
    decimal? AvgTargetKcal,
    decimal? AvgVsExpenditureKcal,
    decimal? AvgVsTargetKcal
);
