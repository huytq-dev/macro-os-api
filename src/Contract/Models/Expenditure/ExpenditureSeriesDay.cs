namespace Contract;

/// <summary>
/// Một ngày của chuỗi expenditure LIÊN TỤC (mỗi ngày từ ngày có dữ liệu đầu tiên tới hôm nay, kể cả ngày chưa có snapshot: giữ TDEE gần nhất).
/// </summary>
/// <param name="ExpenditureKcal">TDEE cuối ngày, làm tròn kcal.</param>
/// <param name="FluxLowKcal">Cận dưới Flux Range (kcal nguyên, MACROOS_FLUX_V1). Luôn <c>FluxLowKcal ≤ ExpenditureKcal ≤ FluxHighKcal</c> khi có dải. null khi chưa đủ dữ liệu: ngày Calibrating (TDEE đóng băng ban đầu) hoặc chuỗi chưa từng Updating (chưa có quan sát hợp lệ); KHÔNG bao giờ là dải giả. Ngày Holding lặp lại dải cuối (không có quan sát mới). FE không được tự sinh dải từ đường Expenditure.</param>
/// <param name="FluxHighKcal">Cận trên Flux Range; cùng quy tắc null như trên.</param>
/// <param name="Status">Updating | Holding (trạng thái thuật toán; dữ liệu cũ chưa dựng lại có thể còn "Calibrating"). Ngày sau snapshot cuối (chưa có dữ liệu mới) giữ TDEE nên là Holding. FE: Updating = vòng tròn; Holding = ô vuông.</param>
/// <param name="ChangeKcal">TDEE hôm nay trừ hôm qua, làm tròn kcal; null ở ngày đầu tiên của chuỗi.</param>
/// <param name="IsCalibrating">Nhãn UI giai đoạn ước lượng ban đầu (&lt; CalibratingDays kể từ ExpenditureStartDate); TÁCH khỏi Status, không đóng băng TDEE.</param>
public sealed record ExpenditureSeriesDay(DateOnly Date, decimal ExpenditureKcal, decimal? FluxLowKcal, decimal? FluxHighKcal, string Status, decimal? ChangeKcal, bool IsCalibrating = false);
