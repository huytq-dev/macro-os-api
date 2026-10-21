namespace Contract;

/// <summary>
/// Một ngày của chuỗi expenditure LIÊN TỤC (mỗi ngày từ ngày có dữ liệu đầu tiên tới hôm nay, kể cả ngày chưa có snapshot: giữ TDEE gần nhất).
/// </summary>
/// <param name="ExpenditureKcal">TDEE cuối ngày, làm tròn kcal.</param>
/// <param name="FluxLowKcal">Cận dưới Flux Range = TDEE − nửa bề rộng (rộng hơn khi cửa sổ dữ liệu ngắn).</param>
/// <param name="FluxHighKcal">Cận trên Flux Range.</param>
/// <param name="Status">Calibrating | Updating | Holding. Ngày sau snapshot cuối (chưa có dữ liệu mới) giữ TDEE nên là Holding, hoặc Calibrating nếu còn trong giai đoạn học. FE: Updating = vòng tròn; Holding/Calibrating = ô vuông.</param>
/// <param name="ChangeKcal">TDEE hôm nay trừ hôm qua, làm tròn kcal; null ở ngày đầu tiên của chuỗi.</param>
public sealed record ExpenditureSeriesDay(DateOnly Date, decimal ExpenditureKcal, decimal FluxLowKcal, decimal FluxHighKcal, string Status, decimal? ChangeKcal);
