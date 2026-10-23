namespace Contract;

/// <summary>
/// Một ngày của chuỗi expenditure LIÊN TỤC (mỗi ngày từ ngày có dữ liệu đầu tiên tới hôm nay, kể cả ngày chưa có snapshot: giữ TDEE gần nhất).
/// </summary>
/// <param name="ExpenditureKcal">TDEE cuối ngày, làm tròn kcal.</param>
/// <param name="FluxLowKcal">Cận dưới Flux Range. LUÔN null cho tới khi Flux được kích hoạt bằng chính sách đã duyệt (dải ± hằng số cũ đã gỡ; FE không được tự sinh dải từ đường Expenditure). Trường giữ lại để tương thích.</param>
/// <param name="FluxHighKcal">Cận trên Flux Range; luôn null như trên.</param>
/// <param name="Status">Updating | Holding (trạng thái thuật toán; dữ liệu cũ chưa dựng lại có thể còn "Calibrating"). Ngày sau snapshot cuối (chưa có dữ liệu mới) giữ TDEE nên là Holding. FE: Updating = vòng tròn; Holding = ô vuông.</param>
/// <param name="ChangeKcal">TDEE hôm nay trừ hôm qua, làm tròn kcal; null ở ngày đầu tiên của chuỗi.</param>
/// <param name="IsCalibrating">Nhãn UI giai đoạn ước lượng ban đầu (&lt; CalibratingDays kể từ ExpenditureStartDate); TÁCH khỏi Status, không đóng băng TDEE.</param>
public sealed record ExpenditureSeriesDay(DateOnly Date, decimal ExpenditureKcal, decimal? FluxLowKcal, decimal? FluxHighKcal, string Status, decimal? ChangeKcal, bool IsCalibrating = false);
