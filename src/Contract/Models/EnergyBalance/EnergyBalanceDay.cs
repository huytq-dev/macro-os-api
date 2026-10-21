namespace Contract;

/// <summary>
/// Một ngày của Energy Balance: kcal đã ăn so với expenditure và với target CỦA NGÀY ĐÓ (không phải target hiện tại). Mọi giá trị làm tròn kcal.
/// </summary>
/// <param name="CaloriesKcal">Tổng kcal ngày; null = chưa log; ngày nhịn ăn = 0. Ngày IsIncomplete vẫn có số nhưng không được tính vào thống kê.</param>
/// <param name="ExpenditureKcal">TDEE của ngày (chuỗi expenditure liên tục, ngày chưa có snapshot giữ TDEE gần nhất); null trước ngày đầu của chuỗi.</param>
/// <param name="TargetKcal">Target calories của ngày theo tuần program hiệu lực tại ngày đó (và dạng ngày trong tuần); null trước tuần program đầu tiên / chưa có program.</param>
/// <param name="VsExpenditureKcal">CaloriesKcal − ExpenditureKcal (âm = Deficit, dương = Surplus); null nếu thiếu một trong hai.</param>
/// <param name="VsTargetKcal">CaloriesKcal − TargetKcal (âm = BelowTarget, dương = AboveTarget); null nếu thiếu một trong hai.</param>
public sealed record EnergyBalanceDay(
    DateOnly Date,
    decimal? CaloriesKcal,
    decimal? ExpenditureKcal,
    decimal? TargetKcal,
    decimal? VsExpenditureKcal,
    decimal? VsTargetKcal,
    bool IsFasting,
    bool IsIncomplete
);
