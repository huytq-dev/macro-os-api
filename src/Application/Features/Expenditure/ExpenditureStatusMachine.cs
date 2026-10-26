namespace Application;

/// <param name="Logged7">Số ngày có log hợp lệ trong cửa sổ log 7 ngày (ngày chưa log ≠ 0 kcal; Partial/0 kcal không nhịn ăn = không hợp lệ).</param>
/// <param name="WeighIns7">Số ngày có cân trong 7 ngày gần nhất tới ngày đánh giá (kể cả hôm nay nếu đã cân).</param>
/// <param name="Run">Số ngày hợp lệ LIÊN TIẾP kết thúc ở ngày cuối của cửa sổ log.</param>
/// <param name="TodayPending">Ngày đánh giá là "hôm nay" (local) và chưa có log hợp lệ: chưa tính là thiếu; cửa sổ log kết thúc ở HÔM QUA.</param>
public sealed record ExpenditureStatusMetrics(int Logged7, int WeighIns7, int Run, bool TodayPending);

/// <summary>
/// MỘT máy trạng thái Updating / Holding / Calibrating dùng chung cho ExpenditureRebuilder, series, insights, check-in (qua snapshot đã lưu).
/// QUY TẮC TỪ MF (VERIFIED, help 74 + V3 blog): duy trì Updating khi log nutrition ≥ 4/7 ngày VÀ cân ≥ 1/7 ngày.
/// RESUME SAU HOLDING = 3 ngày hợp lệ liên tiếp là CHÍNH SÁCH MACROOS, KHÔNG có nguồn MF: help 109 chỉ nói thiếu cân thì "one new weight entry" là đủ chạy lại; help 110 nói thiếu log thì "until you start logging consistently again" (không có ngưỡng số, UNKNOWN).
/// Chưa đổi hành vi (chờ user duyệt, xem Docs/Design/Specs/MF_Formulas_Research_2026-10-26.md §5.1).
/// KẾT HỢP LÀ CHÍNH SÁCH MACROOS (MF không định nghĩa "consistent tracking"): CanMaintain = weighIn7 ≥ 1 ∧ logged7 ≥ 4; CanResume = weighIn7 ≥ 1 ∧ ≥ 3 ngày hợp lệ liên tiếp.
/// Updating → Holding khi CanMaintain sai; Holding → Updating khi CanResume ∨ CanMaintain; Calibrating (chuỗi mới chưa từng Updating) → Updating cùng điều kiện, nếu không giữ Calibrating.
/// Không có ân hạn (G0). Hôm nay chưa xong (local, chưa log hợp lệ): KHÔNG tính là thiếu; Updating giữ nguyên (không lật Holding chỉ vì qua nửa đêm); Holding/Calibrating được đánh giá lại với cửa sổ log kết thúc hôm qua + cân hôm nay,
/// nên có dữ liệu mới hôm nay (cân, hoặc log hợp lệ) vẫn có thể resume ngay. Trạng thái trước = status của snapshot ngày trước (không cần schema). Hàm thuần.
/// </summary>
public static class ExpenditureStatusMachine
{
    public const int ResumeConsecutiveDays = 3;

    /// <param name="days">Các ngày LIÊN TIẾP theo lịch kết thúc ở ngày đánh giá (khuyến nghị 8 ngày: đủ 7 ngày log kết thúc hôm qua khi hôm nay chưa xong); ngày thiếu trong danh sách = không log/không cân.</param>
    public static ExpenditureStatusMetrics Metrics(IReadOnlyList<ExpenditureDay> days, DateOnly localToday)
    {
        if (days.Count == 0) return new(0, 0, 0, false);
        var last = days[^1];
        var pending = last.Date == localToday && !last.KcalIn.HasValue;
        var logWindow = (pending ? days.Take(days.Count - 1) : days).TakeLast(ExpenditureCalculator.RuleDays).ToList();
        var logged = logWindow.Count(d => d.KcalIn.HasValue);
        var run = 0;
        for (var i = logWindow.Count - 1; i >= 0 && logWindow[i].KcalIn.HasValue; i--) run++;
        var weighed = days.TakeLast(ExpenditureCalculator.RuleDays).Count(d => d.TrendKg.HasValue);
        return new(logged, weighed, run, pending);
    }

    public static bool CanMaintain(ExpenditureStatusMetrics m) => m.WeighIns7 >= ExpenditureCalculator.MinWeighInDaysPer7 && m.Logged7 >= ExpenditureCalculator.MinLoggedDaysPer7;

    public static bool CanResume(ExpenditureStatusMetrics m) => m.WeighIns7 >= ExpenditureCalculator.MinWeighInDaysPer7 && m.Run >= ResumeConsecutiveDays;

    /// <param name="previous">Status của ngày trước (snapshot đã lưu); null = chuỗi mới (Calibrating).</param>
    public static ExpenditureStatus Next(ExpenditureStatus? previous, IReadOnlyList<ExpenditureDay> days, DateOnly localToday)
    {
        var m = Metrics(days, localToday);
        var prev = previous ?? ExpenditureStatus.Calibrating;
        // Hôm nay chưa xong và chưa có log hợp lệ: đang Updating thì giữ nguyên (chưa có ngày nào hoàn tất làm điều kiện duy trì sai)
        if (m.TodayPending && prev == ExpenditureStatus.Updating) return ExpenditureStatus.Updating;
        var ok = CanMaintain(m) || (prev != ExpenditureStatus.Updating && CanResume(m));
        if (prev == ExpenditureStatus.Updating) return ok ? ExpenditureStatus.Updating : ExpenditureStatus.Holding;
        return ok ? ExpenditureStatus.Updating : prev;   // Holding/Calibrating giữ nguyên nhãn cho tới khi đủ điều kiện
    }

    /// <summary>Lý do chưa Updating (dạng câu cho dashboard); null nếu đủ điều kiện duy trì hoặc resume ở cửa sổ này.</summary>
    public static string? HoldingReason(ExpenditureStatusMetrics m)
    {
        if (CanMaintain(m) || CanResume(m)) return null;
        return string.Join("; ", new[]
        {
            m.Logged7 < ExpenditureCalculator.MinLoggedDaysPer7 && m.Run < ResumeConsecutiveDays
                ? $"Chưa đủ log: {m.Logged7}/{ExpenditureCalculator.RuleDays} ngày gần nhất (cần ≥ {ExpenditureCalculator.MinLoggedDaysPer7}, hoặc {ResumeConsecutiveDays} ngày liên tiếp)" : null,
            m.WeighIns7 < ExpenditureCalculator.MinWeighInDaysPer7 ? "Không có lần cân nào trong 7 ngày" : null,
        }.Where(s => s is not null));
    }

    public const string CodeNeedsTwoWeighIns = "NeedsTwoWeighIns", CodeNoLogsBetweenWeighIns = "NoLogsBetweenWeighIns";

    /// <summary>
    /// Vì sao <see cref="ExpenditureCalculator.ComputeRaw"/> không tính được TDEE mới dù ĐỦ điều kiện 4/7 log + 1/7 cân (khi đó ExpenditureRebuilder hạ Updating → Holding mà máy trạng thái không biết):
    /// thiếu độ đổi cân nặng (cần ≥ 2 lần cân có trend trong cửa sổ tính, kể từ ngày bắt đầu expenditure) hoặc không có ngày log hợp lệ giữa lần cân đầu và cuối. null = tính được.
    /// </summary>
    public static (string Code, string Message)? RawBlocker(IReadOnlyList<ExpenditureDay> window, int windowDays)
    {
        var weighed = window.Where(d => d.TrendKg.HasValue).ToList();
        if (weighed.Count < 2)
            return (CodeNeedsTwoWeighIns, $"Cần ít nhất 2 lần cân trong {windowDays} ngày gần nhất (kể từ ngày bắt đầu tính) để đo thay đổi cân nặng; hiện có {weighed.Count}");
        var first = weighed[0]; var last = weighed[^1];
        if (!window.Any(d => d.KcalIn.HasValue && d.Date >= first.Date && d.Date <= last.Date))
            return (CodeNoLogsBetweenWeighIns, "Không có ngày log hợp lệ nào giữa lần cân đầu và lần cân cuối trong cửa sổ tính");
        return null;
    }

    /// <summary>Mã lý do Holding: MissingLogs (chưa đủ cả 4/7 lẫn 3 ngày liên tiếp), RecoveringLogs (đang có 1–2 ngày hợp lệ liên tiếp, chưa đủ 3), NoRecentWeighIn.</summary>
    public static List<string> HoldingReasonCodes(ExpenditureStatusMetrics m)
    {
        var codes = new List<string>();
        if (m.Logged7 < ExpenditureCalculator.MinLoggedDaysPer7 && m.Run < ResumeConsecutiveDays) codes.Add("MissingLogs");
        if (m.Run is >= 1 and < ResumeConsecutiveDays && m.Logged7 < ExpenditureCalculator.MinLoggedDaysPer7) codes.Add("RecoveringLogs");
        if (m.WeighIns7 < ExpenditureCalculator.MinWeighInDaysPer7) codes.Add("NoRecentWeighIn");
        return codes;
    }

    /// <summary>
    /// Số ngày hợp lệ liên tiếp còn thiếu để resume NẾU user tiếp tục log hợp lệ mỗi ngày (ước tính, không phải cam kết); null khi không áp dụng
    /// (đang đủ điều kiện, hoặc chưa có cân trong 7 ngày: khi đó log đủ vẫn chưa resume cho tới khi có cân).
    /// </summary>
    public static int? ResumeInDays(ExpenditureStatusMetrics m)
    {
        if (CanMaintain(m) || CanResume(m) || m.WeighIns7 < ExpenditureCalculator.MinWeighInDaysPer7) return null;
        return Math.Max(1, ResumeConsecutiveDays - m.Run);
    }
}
