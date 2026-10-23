namespace Application;

/// <summary>
/// Target hiệu lực của một ngày. D2: tuần có snapshot 7 ngày (<see cref="MacroProgramWeek.DaySnapshotRevision"/>) đọc từ snapshot của chính phiên bản tuần đó, nên lịch sử không đổi khi layout ngày/shift/TDEE đổi sau này.
/// Tuần cũ trước D2 (không có snapshot, hoặc chưa nạp <c>DayTargetSnapshots</c>) FALLBACK về dòng MacroProgramDayTarget + shift HIỆN TẠI và gắn <see cref="LegacyUnverified"/>: số đó KHÔNG được coi là đã kiểm chứng.
/// MỌI nơi đọc target (nhật ký, dashboard, snapshot, check-in) phải đi qua đây để không lệch nhau. Hàm thuần, không I/O; cần nạp sẵn program.DayTargets và week.DayTargetSnapshots.
/// </summary>
public static class ProgramTargetResolver
{
    /// <summary>Provenance của target MỘT NGÀY (field <c>targetProvenance</c> trên mọi API trả target theo ngày): VERIFIED = từ snapshot 7 ngày của đúng phiên bản tuần hiệu lực; LEGACY_UNVERIFIED = tuần trước D2, dựng từ cấu hình hiện tại (tham khảo, không chính thức).</summary>
    public const string Verified = "VERIFIED", LegacyUnverified = "LEGACY_UNVERIFIED";

    /// <summary>
    /// Target đang hiệu lực / kế hoạch của CHƯƠNG TRÌNH ĐANG HOẠT ĐỘNG (không phải lịch sử, không cần snapshot S26): CURRENT_ACTIVE = hôm nay (ngày local của user), PLANNED = ngày sau hôm nay.
    /// Giá trị lấy từ dòng ngày + shift hiện tại của program (cùng nguồn thẻ Strategy), nên đổi sau check-in/đổi goal. Chỉ VERIFIED / LEGACY_UNVERIFIED mới là provenance LỊCH SỬ (ngày &lt; hôm nay). FE chỉ ẩn LEGACY_UNVERIFIED.
    /// </summary>
    public const string CurrentActive = "CURRENT_ACTIVE", Planned = "PLANNED";

    /// <summary>Provenance (không phụ thuộc giá trị) của target ngày <paramref name="date"/>; <paramref name="today"/> = hôm nay local của user, null = không biết (coi mọi ngày là lịch sử).</summary>
    public static string ProvenanceFor(MacroProgramWeek week, MacroProgram program, DateOnly date, DateOnly? today)
        => today is { } t && program.IsActive && date >= t ? (date == t ? CurrentActive : Planned)
         : SnapshotOf(week, date.DayOfWeek) is null ? LegacyUnverified : Verified;

    /// <summary>Provenance LỊCH SỬ theo thứ (không biết ngày/hôm nay): VERIFIED khi tuần có snapshot, ngược lại LEGACY_UNVERIFIED.</summary>
    public static string ProvenanceOf(MacroProgramWeek week, DayOfWeek day) => SnapshotOf(week, day) is null ? LegacyUnverified : Verified;

    public static readonly DayOfWeek[] WeekOrder =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public static ProgramWeekModel ForDay(MacroProgramWeek week, MacroProgram program, DayOfWeek day)
    {
        if (SnapshotOf(week, day) is { } snap)
            return week.Adapt<ProgramWeekModel>() with
            {
                CaloriesTarget = snap.CaloriesTarget, ProteinTarget = snap.ProteinTarget, CarbsTarget = snap.CarbsTarget, FatTarget = snap.FatTarget, TargetProvenance = Verified,
            };
        return Live(week, program, day) with { TargetProvenance = LegacyUnverified };
    }

    /// <summary>Dòng snapshot hiện hành của ngày trong phiên bản tuần; null = tuần cũ trước D2 (hoặc snapshot chưa được nạp).</summary>
    public static MacroProgramWeekDayTarget? SnapshotOf(MacroProgramWeek week, DayOfWeek day)
        => week.DaySnapshotRevision is { } rev ? week.DayTargetSnapshots.FirstOrDefault(s => s.SnapshotRevision == rev && s.DayOfWeek == day) : null;

    /// <summary>Target của ngày theo cấu hình HIỆN TẠI của program (dòng ngày + shift), không đọc snapshot: dùng để CHỤP snapshot mới (<see cref="WeekDaySnapshots.Capture"/>) và làm fallback legacy.</summary>
    public static ProgramWeekModel Live(MacroProgramWeek week, MacroProgram program, DayOfWeek day)
    {
        var model = week.Adapt<ProgramWeekModel>();
        var row = program.DayTargets.FirstOrDefault(d => d.DayOfWeek == day);
        if (row is null)
        {
            var shifted = CalorieShiftCalculator.ForDay(
                new MacroTargets(model.CaloriesTarget, model.ProteinTarget, model.CarbsTarget, model.FatTarget),
                program.CalorieShiftMode, program.CalorieShiftPercent, program.CalorieShiftDays, day);
            return model with { CaloriesTarget = shifted.Calories, CarbsTarget = shifted.CarbsG };
        }

        if (row.IsFastingDay) return model with { CaloriesTarget = 0, ProteinTarget = 0, CarbsTarget = 0, FatTarget = 0 };
        return model with
        {
            CaloriesTarget = row.CaloriesTarget ?? model.CaloriesTarget,
            ProteinTarget = row.ProteinTarget ?? model.ProteinTarget,
            CarbsTarget = row.CarbsTarget ?? model.CarbsTarget,
            FatTarget = row.FatTarget ?? model.FatTarget,
        };
    }

    /// <param name="today">Hôm nay local của user. Có và <paramref name="date"/> ≥ today và program đang hoạt động ⇒ target CURRENT_ACTIVE / PLANNED từ cấu hình hiện hành (không cần snapshot);
    /// ngày &lt; today (hoặc today = null) ⇒ lịch sử: snapshot VERIFIED, không có thì fallback + LEGACY_UNVERIFIED (không dùng dòng hiện tại để tuyên bố lịch sử chính xác).</param>
    public static ProgramWeekModel ForDate(MacroProgramWeek week, MacroProgram program, DateOnly date, DateOnly? today = null)
        => today is { } t && program.IsActive && date >= t
            ? Live(week, program, date.DayOfWeek) with { TargetProvenance = ProvenanceFor(week, program, date, today) }
            : ForDay(week, program, date.DayOfWeek);

    /// <summary>
    /// 7 ngày Thứ 2 → Chủ nhật theo tuần <paramref name="week"/> (cho thẻ Program / màn Review). Với <paramref name="today"/>: mỗi thứ được gắn provenance theo NGÀY LỊCH của tuần lịch chứa hôm nay
    /// (ngày đã qua = lịch sử VERIFIED/LEGACY_UNVERIFIED; hôm nay = CURRENT_ACTIVE; sau đó = PLANNED); giá trị vẫn là kế hoạch hiện hành của tuần.
    /// </summary>
    public static List<ProgramDayModel> Days(MacroProgramWeek week, MacroProgram program, DateOnly? today = null)
    {
        var monday = today is { } t ? t.AddDays(-(((int)t.DayOfWeek + 6) % 7)) : (DateOnly?)null;
        return WeekOrder.Select((day, i) =>
        {
            var model = ForDay(week, program, day);   // giá trị kế hoạch của tuần này (snapshot nếu có, không thì cấu hình hiện hành), như trước
            var provenance = monday is { } m ? ProvenanceFor(week, program, m.AddDays(i), today) : model.TargetProvenance;
            var locked = SnapshotOf(week, day)?.IsLocked ?? program.DayTargets.FirstOrDefault(d => d.DayOfWeek == day)?.IsLocked ?? false;
            return new ProgramDayModel(day.ToString(), model.CaloriesTarget, model.ProteinTarget, model.CarbsTarget, model.FatTarget, locked, provenance);
        }).ToList();
    }
}
