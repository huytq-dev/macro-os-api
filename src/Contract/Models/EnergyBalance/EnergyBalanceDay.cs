namespace Contract;

/// <summary>
/// Một ngày của Energy Balance: kcal đã ăn so với expenditure và với target CỦA NGÀY ĐÓ (không phải target hiện tại). Mọi giá trị làm tròn kcal.
/// </summary>
/// <param name="CaloriesKcal">Tổng kcal ngày; null = chưa log; ngày nhịn ăn = 0. Ngày IsIncomplete vẫn có số nhưng không được tính vào thống kê.</param>
/// <param name="ExpenditureKcal">TDEE của ngày (chuỗi expenditure liên tục, ngày chưa có snapshot giữ TDEE gần nhất); null trước ngày đầu của chuỗi.</param>
/// <param name="TargetKcal">Target calories của ngày theo tuần program hiệu lực tại ngày đó (và dạng ngày trong tuần); null trước tuần program đầu tiên / chưa có program.</param>
/// <param name="VsExpenditureKcal">CaloriesKcal − ExpenditureKcal (âm = Deficit, dương = Surplus); null nếu thiếu một trong hai, hoặc ngày bị loại khỏi thống kê (đánh dấu chưa đủ, hoặc có entry nhưng 0 kcal không nhịn ăn: không phải ngày hợp lệ, không được trình bày như thâm hụt thật). Ngày có kcal nhưng chưa có expenditure thì vsExpenditure null và vsTarget vẫn có. FE: vs* khác null = ngày được tính của tab đó.</param>
/// <param name="VsTargetKcal">CaloriesKcal − TargetKcal (âm = BelowTarget, dương = AboveTarget); null nếu thiếu một trong hai, hoặc ngày 0 kcal không nhịn ăn như trên.</param>
public sealed record EnergyBalanceDay(
    DateOnly Date,
    decimal? CaloriesKcal,
    decimal? ExpenditureKcal,
    decimal? TargetKcal,
    decimal? VsExpenditureKcal,
    decimal? VsTargetKcal,
    bool IsFasting,
    bool IsIncomplete,
    string? TargetProvenance = null
);
// TargetProvenance: VERIFIED | LEGACY_UNVERIFIED | null (chưa có target). LEGACY_UNVERIFIED: TargetKcal/VsTargetKcal chỉ để THAM KHẢO (nhãn "Chưa xác minh", không badge đạt/không đạt);
// ngày đó KHÔNG vào mọi thống kê so với target (Summary/Bucket/Relative loại và đếm vào UnverifiedTargetDays). VsExpenditureKcal không phụ thuộc target nên giữ nguyên.
