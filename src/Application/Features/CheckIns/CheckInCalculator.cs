namespace Application;

/// <summary>ALG-04 (thuần): target tuần mới từ TDEE[t], rate mục tiêu theo trend hiện tại, hedge ±Δmax, sàn kcal, protein giữ nguyên trừ khi cân đổi > 2 kg (ADR-005).</summary>
public static class CheckInCalculator
{
    public static CheckInProposal Propose(MacroProgram p, decimal trendNow, decimal tdee, MacroProgramWeek prev, AlgorithmOptions opt)
    {
        var rateTargetKg = Math.Round(p.WeeklyWeightChangeTargetPercent / 100m * trendNow, 3);
        var raw = (int)Math.Round(tdee - rateTargetKg * opt.KcalPerKg / 7m);

        var delta = Math.Clamp(raw - prev.CaloriesTarget, -opt.MaxWeeklyDeltaKcal, opt.MaxWeeklyDeltaKcal);
        var clamped = delta != raw - prev.CaloriesTarget;
        var kcal = prev.CaloriesTarget + delta;

        var floorApplied = false;
        if (p.ProgramType == ProgramType.Coached && p.CalorieFloorKcal is { } floor && kcal < floor)
        {
            kcal = floor;
            floorApplied = true;
        }

        // ADR-005: protein giữ nguyên trừ khi cân (suy từ protein tuần trước) lệch > 2 kg
        var gPerKg = p.ProteinGPerKgBodyweight;
        var impliedPrevKg = gPerKg > 0 ? prev.ProteinTarget / gPerKg : trendNow;
        var protein = p.FixedProteinGrams ?? (Math.Abs(impliedPrevKg - trendNow) > 2m ? Math.Round(gPerKg * trendNow, 1) : prev.ProteinTarget);

        var targets = MacroTargetCalculator.SplitMacros(kcal, trendNow, gPerKg, protein, p.DietStyle);
        return new CheckInProposal(prev.CaloriesTarget, raw, kcal, kcal - prev.CaloriesTarget, clamped, floorApplied, rateTargetKg, targets);
    }

    /// <summary>
    /// Ngày check-in kế tiếp: lần đầu = weekday đầu tiên sau StartDate; sau đó = weekday đầu tiên SAU (check-in cuối + 2 ngày).
    /// Due khi today ≥ ngày đó − 2 (sớm ≤ 2 ngày). Cộng thêm 2 ngày để check-in sớm không làm kỳ kế rơi ngay vào cửa sổ "sớm" của chính nó
    /// (vd check-in Thứ 7 cho weekday Thứ 2 phải ra Thứ 2 tuần sau, không phải 2 ngày sau).
    /// </summary>
    public static (DateOnly NextDate, bool IsDue) Next(DateOnly today, DayOfWeek weekday, DateOnly startDate, DateOnly? lastCheckIn)
    {
        var after = lastCheckIn?.AddDays(EarlyDays) ?? startDate;
        var next = after.AddDays(1);
        while (next.DayOfWeek != weekday) next = next.AddDays(1);
        return (next, today >= next.AddDays(-EarlyDays));
    }

    /// <summary>Check-in được phép sớm tối đa số ngày này (0 = chỉ đúng hạn như MacroFactor).</summary>
    public const int EarlyDays = 2;

    /// <summary>
    /// Mã lý do (i18n ở FE) cho đề xuất tuần: trạng thái expenditure trước, rồi tốc độ thực tế so với mục tiêu, rồi các giới hạn đã áp.
    /// Calibrating/Holding không có đề xuất nên chỉ có mã trạng thái (+ INCOMPLETE_DAYS nếu user đã bỏ ngày).
    /// </summary>
    /// <summary>Thêm vào ReasonCodes của preview khi đề xuất không khả thi (xem <c>Feasibility</c>): Accept bị từ chối, target đã chấp nhận được giữ.</summary>
    public const string InfeasibleProposal = "INFEASIBLE_PROPOSAL";

    public const string FasterThanTarget ="FASTER_THAN_TARGET", SlowerThanTarget = "SLOWER_THAN_TARGET", OnTarget = "ON_TARGET", LosingWeight = "LOSING_WEIGHT", GainingWeight = "GAINING_WEIGHT";

    /// <summary>
    /// Phân loại tốc độ thực tế so với mục tiêu THEO HƯỚNG của goal (hàm thuần, CHỈ để hiển thị: không bao giờ dùng để chỉnh kcal). Cả hai tốc độ theo quy ước program: dương = giảm, âm = tăng (kg/tuần).
    /// Loss: giảm nhiều hơn mục tiêu = FASTER. Gain/MuscleGain: tăng nhiều hơn mục tiêu = FASTER (tăng cân là âm nên <c>actual &lt; target</c>). Maintain: không có FASTER/SLOWER theo hướng giảm cân:
    /// chỉ ON_TARGET (đúng bằng 0), LOSING_WEIGHT hoặc GAINING_WEIGHT. ON_TARGET vẫn là so sánh bằng chính xác: dung sai CHƯA được duyệt (đề xuất chính sách riêng, xem Docs/Design/Verification/CheckIn_OnTarget_Tolerance_Proposal.md).
    /// </summary>
    public static string Classify(GoalDirection direction, decimal rateActual, decimal rateTarget) => direction switch
    {
        GoalDirection.Loss => rateActual > rateTarget ? FasterThanTarget : rateActual < rateTarget ? SlowerThanTarget : OnTarget,
        GoalDirection.Gain => rateActual < rateTarget ? FasterThanTarget : rateActual > rateTarget ? SlowerThanTarget : OnTarget,
        _ => rateActual > 0m ? LosingWeight : rateActual < 0m ? GainingWeight : OnTarget,
    };

    /// <param name="emitRateVerdict">
    /// false (MẶC ĐỊNH, quyết định của user): KHÔNG phát FASTER/SLOWER/ON_TARGET/LOSING_WEIGHT/GAINING_WEIGHT. Phán quyết này dựa trên tốc độ two-point 7 ngày chưa được kiểm chứng (SD 0.07–0.34 kg/tuần, sai ~50 % trong mô phỏng,
    /// kể cả khi đã qua cổng TRA + guard G = 14) nên BE trả trạng thái trung lập (không có mã phân loại; thiếu mã là trạng thái hợp lệ có sẵn, ví dụ Calibrating/Holding) thay vì để FE che nhãn.
    /// <see cref="Classify"/> (đã sửa dấu theo hướng goal) được giữ nguyên cho bộ phân loại tương lai; phân loại chỉ là thông tin, không bao giờ đổi kcal, macro, expenditure hay đề xuất.
    /// </param>
    public static IReadOnlyList<string> ReasonCodes(CheckInProposal pr, ExpenditureStatus status, decimal? rateActual, decimal rateTarget, int daysMarkedIncomplete, GoalDirection direction = GoalDirection.Loss, bool emitRateVerdict = false)
    {
        var codes = new List<string>();
        if (status == ExpenditureStatus.Calibrating) codes.Add("CALIBRATING");
        if (status == ExpenditureStatus.Holding) codes.Add("HOLDING");
        if (daysMarkedIncomplete > 0) codes.Add("INCOMPLETE_DAYS");
        if (status != ExpenditureStatus.Updating) return codes;

        if (emitRateVerdict && rateActual is { } ra) codes.Add(Classify(direction, ra, rateTarget));
        if (pr.Clamped) codes.Add("CLAMPED");
        if (pr.FloorApplied) codes.Add("FLOOR_APPLIED");
        return codes;
    }

    /// <summary>Số ngày từ hôm nay tới ngày check-in kế tiếp; quá hạn (đã qua ngày đó mà chưa check-in) tính là 0.</summary>
    public static int DaysUntil(DateOnly today, DateOnly nextCheckIn) => Math.Max(0, nextCheckIn.DayNumber - today.DayNumber);

    public static string Reason(CheckInProposal pr, ExpenditureStatus status, decimal? rateActual, decimal rateTarget, GoalDirection direction = GoalDirection.Loss, bool emitRateVerdict = false)
    {
        if (status == ExpenditureStatus.Calibrating) return "Đang hiệu chỉnh expenditure: thay đổi nhỏ cho tới khi đủ dữ liệu.";
        var parts = new List<string>();
        if (rateActual is { } rn && !emitRateVerdict)
        {
            // Thông tin tiến độ TRUNG LẬP: chỉ số đo được (có dấu: âm = giảm cân) và mục tiêu, không kết luận nhanh/chậm/đúng mục tiêu
            parts.Add($"Cân thay đổi {(-rn).ToString("+0.00;-0.00;0.00", System.Globalization.CultureInfo.InvariantCulture)} kg/tuần (mục tiêu {(-rateTarget).ToString("+0.00;-0.00;0.00", System.Globalization.CultureInfo.InvariantCulture)} kg/tuần); chưa đủ cơ sở để so sánh với mục tiêu");
        }
        else if (rateActual is { } ra)
        {
            // Mô tả theo hướng goal (cùng phân loại với ReasonCodes); Gain/Maintain không dùng chữ "giảm" và không khẳng định hướng chỉnh kcal (kcal do công thức đề xuất quyết định, hiển thị riêng)
            var code = Classify(direction, ra, rateTarget);
            parts.Add(direction switch
            {
                GoalDirection.Loss => code == FasterThanTarget ? $"Giảm nhanh hơn mục tiêu ({ra:0.00} vs {rateTarget:0.00} kg/tuần) → tăng kcal"
                                    : code == SlowerThanTarget ? $"Giảm chậm hơn mục tiêu ({ra:0.00} vs {rateTarget:0.00} kg/tuần) → giảm kcal"
                                    : "Đúng tốc độ mục tiêu",
                GoalDirection.Gain => code == FasterThanTarget ? $"Tăng nhanh hơn mục tiêu ({-ra:0.00} vs {-rateTarget:0.00} kg/tuần)"
                                    : code == SlowerThanTarget ? $"Tăng chậm hơn mục tiêu ({-ra:0.00} vs {-rateTarget:0.00} kg/tuần)"
                                    : "Đúng tốc độ mục tiêu",
                _ => code == LosingWeight ? $"Cân đang giảm {ra:0.00} kg/tuần (mục tiêu giữ cân)"
                   : code == GainingWeight ? $"Cân đang tăng {-ra:0.00} kg/tuần (mục tiêu giữ cân)"
                   : "Đúng mục tiêu giữ cân",
            });
        }
        if (pr.Clamped) parts.Add("Giới hạn thay đổi tối đa mỗi tuần");
        if (pr.FloorApplied) parts.Add("Chạm sàn kcal");
        if (status == ExpenditureStatus.Holding) parts.Add("Expenditure đang Holding: dùng TDEE cuối");
        return parts.Count == 0 ? "Giữ nguyên target" : string.Join("; ", parts);
    }
}
