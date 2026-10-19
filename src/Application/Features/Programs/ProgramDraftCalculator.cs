namespace Application;

/// <param name="KcalMax">Trần kcal/ngày ở editor ("set between 0 and N kcal").</param>
/// <param name="ProteinPerKgMax">Trần protein g/kg cân nặng.</param>
public sealed record ProgramLimits(int KcalMax, decimal ProteinPerKgMax);

/// <summary>
/// Dùng chung cho wizard New Program: xem trước (không ghi DB) và tạo thật cùng một công thức, nên số ở màn Review khớp số được lưu. Hàm thuần.
/// </summary>
public static class ProgramDraftCalculator
{
    // kcal từ macro lệch quá ngưỡng này so với kcal nhập → cảnh báo (không chặn, như MacroFactor nhưng có cảnh báo)
    private const decimal MacroMismatchRatio = 0.15m;

    public static ProgramLimits Limits(int tdee, AlgorithmOptions opt)
        => new(Math.Max(3000, (int)(Math.Round(tdee * opt.DayKcalMaxFactor / 100m, MidpointRounding.AwayFromZero) * 100m)), opt.MaxProteinGPerKg);

    /// <summary>Program chưa lưu từ prefs của wizard (cùng cách <see cref="CreateProgramCommand"/> dựng). Id mới mỗi lần gọi.</summary>
    public static MacroProgram BuildProgram(CreateProgramCommand r, UserProfile profile, decimal weightKg, DateOnly today)
    {
        var ratePercent = r.GoalType switch
        {
            GoalType.Maintenance => 0,
            GoalType.WeightGain  => -Math.Abs(r.WeeklyWeightChangeTargetPercent),
            _                    => Math.Abs(r.WeeklyWeightChangeTargetPercent),
        };

        var shifted = r.CalorieShiftMode is { } mode && mode != CalorieShiftMode.Even;
        return new MacroProgram
        {
            Id = Guid.NewGuid(),
            UserId = profile.UserId,
            ProgramType = r.ProgramType,
            GoalType = r.GoalType,
            DietStyle = r.DietStyle,
            WeeklyWeightChangeTargetPercent = ratePercent,
            WeeklyWeightChangeTargetKg = Math.Round(-ratePercent / 100m * weightKg, 2),
            ProteinLevel = r.ProteinLevel,
            ProteinGPerKgBodyweight = MacroTargetCalculator.ProteinGPerKg(r.ProteinLevel, r.TrainingType ?? profile.TrainingType),
            CalorieFloorKcal = r.CalorieFloorKcal,
            CheckInWeekday = r.CheckInWeekday ?? today.DayOfWeek,
            StartDate = today,
            ExpenditureStartDate = today,
            ManualInitialTdeeKcal = r.ManualInitialTdeeKcal,
            TargetWeightKg = r.TargetWeightKg,
            CalorieShiftMode = shifted ? r.CalorieShiftMode!.Value : CalorieShiftMode.Even,
            CalorieShiftPercent = shifted ? r.CalorieShiftPercent : null,
            CalorieShiftDays = shifted ? CalorieShiftCalculator.ToFlags(r.CalorieShiftDays) : DaysOfWeek.None,
        };
    }

    /// <summary>
    /// 7 ngày của program: user đã đặt (dayTargets, Collaborative/Manual) thì dùng đúng như vậy (sau khi kiểm giới hạn), không thì do thuật toán tính
    /// (kèm dịch kcal). Manual bắt buộc có dayTargets vì không có thuật toán nào tính hộ.
    /// </summary>
    public static List<DayTargetInput> Days(CreateProgramCommand r, MacroProgram program, decimal weightKg, int tdee, AlgorithmOptions opt)
    {
        if (r.DayTargets is { Count: > 0 } given)
        {
            Validate(given, Limits(tdee, opt), weightKg);
            return given.OrderBy(d => Array.IndexOf(ProgramTargetResolver.WeekOrder, d.DayOfWeek)).ToList();
        }
        if (program.ProgramType == ProgramType.Manual)
            throw new BadRequestException(ExceptionMessages.ManualProgramNeedsSevenDays);

        var targets = ProgramWeekBuilder.Compute(program, weightKg, tdee, opt);
        return ProgramTargetResolver.WeekOrder.Select(day =>
        {
            var t = CalorieShiftCalculator.ForDay(targets, program.CalorieShiftMode, program.CalorieShiftPercent, program.CalorieShiftDays, day);
            return new DayTargetInput(day, t.Calories, t.ProteinG, t.CarbsG, t.FatG);
        }).ToList();
    }

    /// <summary>Giới hạn phụ thuộc người dùng (TDEE, cân nặng) nên không đặt được trong validator tĩnh.</summary>
    public static void Validate(IEnumerable<DayTargetInput> days, ProgramLimits limits, decimal weightKg)
    {
        foreach (var d in days)
        {
            if (d.Calories > limits.KcalMax)
                throw new BadRequestException(ExceptionMessages.DayKcalMax(d.DayOfWeek, limits.KcalMax));
            if (d.ProteinG > limits.ProteinPerKgMax * weightKg)
                throw new BadRequestException(ExceptionMessages.DayProteinMax(d.DayOfWeek, limits.ProteinPerKgMax));
        }
    }

    /// <summary>Tổng hợp cho màn Review. kg/% là SUY RA từ kcal trung bình so với TDEE (dương = tăng cân) nên đúng cả khi sàn kcal hoặc ngày nhập tay thay đổi kết quả.</summary>
    public static ProgramDraftPreviewResponse Preview(
        IReadOnlyList<DayTargetInput> days, int tdee, decimal weightKg, decimal? targetWeightKg, int? floorKcal, int kcalPerKg, DateOnly today)
    {
        var avgKcal = (int)Math.Round(days.Average(d => (decimal)d.Calories), MidpointRounding.ToEven);
        var avgP = Math.Round(days.Average(d => d.ProteinG), 1);
        var avgC = Math.Round(days.Average(d => d.CarbsG), 1);
        var avgF = Math.Round(days.Average(d => d.FatG), 1);

        var weeklyKg = Math.Round((avgKcal - tdee) * 7m / kcalPerKg, 2);
        DateOnly? end = null;
        if (targetWeightKg is { } target && weeklyKg != 0)
        {
            var delta = target - weightKg;
            if (Math.Sign(delta) == Math.Sign(weeklyKg))
                end = today.AddDays((int)Math.Ceiling(Math.Abs(delta / weeklyKg) * 7m));
        }

        var warnings = new List<ProgramDraftWarning>();
        foreach (var d in days.Where(d => !d.IsFastingDay))
        {
            var macroKcal = 4m * d.ProteinG + 4m * d.CarbsG + 9m * d.FatG;
            if (d.Calories > 0 && Math.Abs(macroKcal - d.Calories) / d.Calories > MacroMismatchRatio)
                warnings.Add(new ProgramDraftWarning("MACRO_KCAL_MISMATCH", d.DayOfWeek.ToString()));
            if (floorKcal is { } floor && d.Calories < floor)
                warnings.Add(new ProgramDraftWarning("BELOW_FLOOR", d.DayOfWeek.ToString()));
        }

        return new ProgramDraftPreviewResponse(
            days.Select(d => new ProgramDayModel(d.DayOfWeek.ToString(), d.IsFastingDay ? 0 : d.Calories, d.ProteinG, d.CarbsG, d.FatG, d.IsLocked)).ToList(),
            avgKcal, days.Sum(d => d.Calories), avgP, avgC, avgF, (int)Math.Round(4m * avgP + 4m * avgC + 9m * avgF),
            tdee, weeklyKg, Math.Round(weeklyKg / weightKg * 100m, 2), end, warnings);
    }
}
