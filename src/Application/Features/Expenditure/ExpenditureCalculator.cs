namespace Application;

/// <summary>ALG-03 (thuần, không DB): TDEE_raw = mean(kcal_in) − Δtrend × kcalPerKg / số ngày; làm mượt EMA β.</summary>
public static class ExpenditureCalculator
{
    /// <summary>
    /// Null khi chưa đủ dữ liệu: &lt; 2 điểm trend hoặc không ngày nào có log hợp lệ TRONG KHOẢNG CÂN [lần cân đầu, lần cân cuối] của cửa sổ.
    /// v6/v7 (sửa EXP-001): lượng ăn trung bình chỉ lấy các ngày hợp lệ trong CÙNG khoảng thời gian với độ đổi trend (hai đầu bao gồm), không phải cả cửa sổ 21 ngày.
    /// Lý do: Δtrend chỉ quan sát giữa hai lần cân, ngày log ngoài khoảng đó không có thay đổi cân tương ứng nên làm lệch TDEE. Trend chỉ có giá trị tại ngày cân (không suy thêm ngày),
    /// nên không có ước lượng trend hợp lệ ngoài hai đầu để giữ lại dữ liệu ăn ngoài khoảng. Mẫu số = số ngày hợp lệ trong khoảng. Giả định chưa kiểm chứng: hai đầu bao gồm (không dịch 1 ngày theo giờ cân buổi sáng).
    /// </summary>
    public static ExpenditureRaw? ComputeRaw(IReadOnlyList<ExpenditureDay> window, int kcalPerKg)
    {
        var weighed = window.Where(d => d.TrendKg.HasValue).ToList();
        if (weighed.Count < 2) return null;

        var first = weighed[0]; var last = weighed[^1];
        var spanDays = last.Date.DayNumber - first.Date.DayNumber;
        if (spanDays <= 0) return null;

        var logged = window.Where(d => d.KcalIn.HasValue && d.Date >= first.Date && d.Date <= last.Date).Select(d => d.KcalIn!.Value).ToList();
        if (logged.Count == 0) return null;
        var avgKcal = logged.Average(); // ngày thiếu trong khoảng = trung bình ngày đã log (BR sai số ~13 %)

        var deltaKg = last.TrendKg!.Value - first.TrendKg!.Value;
        var tdeeRaw = avgKcal - deltaKg * kcalPerKg / spanDays;
        var rate = deltaKg / spanDays * 7;
        return new ExpenditureRaw(Math.Round(tdeeRaw, 2), Math.Round(avgKcal, 2), Math.Round(rate, 3), logged.Count);
    }

    /// <summary>Nửa bề rộng Flux Range quanh TDEE: sai số trend (kg) quy ra kcal/ngày trên cửa sổ dữ liệu; ít dữ liệu (tối thiểu tính như 7 ngày) → dải rộng hơn.</summary>
    public static decimal FluxHalfWidth(int windowDays, int kcalPerKg, decimal trendErrorKg)
        => Math.Round(trendErrorKg * kcalPerKg / Math.Max(windowDays, 7));

    /// <summary>Biến thiên TDEE so với <paramref name="days"/> ngày trước; Waiting khi thiếu điểm ở mốc đó hoặc mốc nằm trước ngày bắt đầu tính.</summary>
    public static ExpenditureChangeModel Change(IReadOnlyDictionary<DateOnly, decimal> tdeeByDate, DateOnly latest, int days, DateOnly? start)
    {
        var past = latest.AddDays(-days);
        if (tdeeByDate.TryGetValue(latest, out var now) && tdeeByDate.TryGetValue(past, out var before) && (start is not { } s || past >= s))
            return new ExpenditureChangeModel(days, Math.Round(now - before), ExpenditureChangeModel.Ready);
        return new ExpenditureChangeModel(days, null, ExpenditureChangeModel.Waiting);
    }

    public static decimal Smooth(decimal prevTdee, decimal rawTdee, decimal beta)
        => Math.Round(beta * rawTdee + (1 - beta) * prevTdee, 2);

    /// <summary>
    /// ST-01: Updating khi 7 ngày gần nhất ≤ 3 ngày thiếu log (kcal hợp lệ) và ≥ 1 lần cân; else Holding. Không còn đóng băng TDEE trong 14 ngày đầu:
    /// học từ ngày 1 của program, cập nhật khi đủ điều kiện này VÀ <see cref="ComputeRaw"/> tính được (không thì Holding, giữ TDEE cũ).
    /// </summary>
    public static ExpenditureStatus Status(IReadOnlyList<ExpenditureDay> last7)
        => HoldingReason(last7) is null ? ExpenditureStatus.Updating : ExpenditureStatus.Holding;

    /// <summary>
    /// Nhãn UI "Calibrating" của giai đoạn ước lượng ban đầu (&lt; calibratingDays kể từ start). CHỈ là nhãn hiển thị, tách khỏi trạng thái thuật toán Updating/Holding:
    /// không đóng băng TDEE và không phải thời điểm hoàn tất chắc chắn (đủ dữ liệu thì cập nhật dù chưa hết nhãn).
    /// </summary>
    public static bool IsCalibrating(DateOnly date, DateOnly start, int calibratingDays) => date.DayNumber - start.DayNumber < calibratingDays;

    /// <summary>Số ngày gần nhất xét điều kiện Updating/Holding.</summary>
    public const int RuleDays = 7;

    /// <summary>Điều kiện giữ Updating: ít nhất 4/7 ngày có log kcal hợp lệ (tối đa 3 ngày thiếu). FE đọc qua /expenditure/insights, không hardcode.</summary>
    public const int MaxMissingLogDays = 3;
    public const int MinLoggedDaysPer7 = RuleDays - MaxMissingLogDays;

    /// <summary>Điều kiện giữ Updating: ít nhất 1 lần cân trong 7 ngày gần nhất.</summary>
    public const int MinWeighInDaysPer7 = 1;

    /// <summary>Lý do Holding cho dashboard; null nếu đủ điều kiện Updating.</summary>
    public static string? HoldingReason(IReadOnlyList<ExpenditureDay> last7)
    {
        var missing = last7.Count(d => !d.KcalIn.HasValue) + Math.Max(0, RuleDays - last7.Count);
        var weighIns = last7.Count(d => d.TrendKg.HasValue);
        if (missing <= MaxMissingLogDays && weighIns >= MinWeighInDaysPer7) return null;
        return string.Join("; ", new[]
        {
            missing > MaxMissingLogDays ? $"Thiếu log {missing}/{RuleDays} ngày gần nhất (tối đa {MaxMissingLogDays})" : null,
            weighIns < MinWeighInDaysPer7 ? "Không có lần cân nào trong 7 ngày" : null,
        }.Where(s => s is not null));
    }
}
