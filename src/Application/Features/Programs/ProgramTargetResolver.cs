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

    public static ProgramWeekModel ForDate(MacroProgramWeek week, MacroProgram program, DateOnly date) => ForDay(week, program, date.DayOfWeek);

    /// <summary>7 ngày Thứ 2 → Chủ nhật theo tuần <paramref name="week"/> (cho thẻ Program / màn Review).</summary>
    public static List<ProgramDayModel> Days(MacroProgramWeek week, MacroProgram program)
        => WeekOrder.Select(day =>
        {
            var t = ForDay(week, program, day);
            var locked = SnapshotOf(week, day)?.IsLocked ?? program.DayTargets.FirstOrDefault(d => d.DayOfWeek == day)?.IsLocked ?? false;
            return new ProgramDayModel(day.ToString(), t.CaloriesTarget, t.ProteinTarget, t.CarbsTarget, t.FatTarget, locked, t.TargetProvenance);
        }).ToList();
}
