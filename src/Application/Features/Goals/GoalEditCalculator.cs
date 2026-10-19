namespace Application;

public enum GoalDirection { Loss, Gain, Maintain }

/// <param name="ProgramRatePercent">Quy ước của MacroProgram: dương = giảm cân, âm = tăng cân, 0 = giữ.</param>
/// <param name="TdeeKcal">null khi chưa có expenditure lẫn TDEE nhập tay.</param>
public sealed record GoalEditState(
    decimal CurrentWeightKg, GoalType GoalType, decimal? GoalTargetKg, DateOnly GoalStartDate,
    ProgramType ProgramType, decimal ProgramRatePercent, int? FloorKcal, int? TdeeKcal);

/// <param name="ProgramRatePercent">Giá trị sẽ ghi vào MacroProgram (dương = giảm).</param>
/// <param name="SignedWeeklyKg">Giá trị sẽ ghi vào Goal/Program.WeeklyWeightChangeTargetKg (âm = giảm).</param>
public sealed record GoalEditResult(GoalEditPreviewResponse Preview, GoalType NewGoalType, decimal ProgramRatePercent, decimal SignedWeeklyKg);

/// <summary>
/// Edit Goal (thuần, không I/O): suy ra loại goal từ đích so với cân hiện tại, kiểm tốc độ trong dải, tính ngân sách khởi điểm và ngày dự kiến.
/// Dùng chung cho xem trước (không ghi DB) và áp dụng để hai bên luôn cùng một con số.
/// </summary>
public static class GoalEditCalculator
{
    /// <summary>Đích lệch cân hiện tại dưới ngưỡng này (bước thước 0.1 kg) coi như giữ cân.</summary>
    public const decimal MaintenanceToleranceKg = 0.1m;
    // Đích quá xa cân hiện tại (nhập nhầm) bị từ chối
    private const decimal MaxTargetDeviation = 0.5m;

    public static GoalDirection DirectionOf(GoalType type) => type switch
    {
        GoalType.WeightLoss => GoalDirection.Loss,
        GoalType.WeightGain or GoalType.MuscleGain => GoalDirection.Gain,
        _ => GoalDirection.Maintain,
    };

    /// <summary>Đích trống hoặc gần bằng cân hiện tại → Maintain; cao hơn → Gain; thấp hơn → Loss.</summary>
    public static GoalDirection Derive(decimal currentKg, decimal? target)
        => target is not { } t || Math.Abs(t - currentKg) < MaintenanceToleranceKg ? GoalDirection.Maintain
            : t > currentKg ? GoalDirection.Gain : GoalDirection.Loss;

    /// <summary>Cùng hướng với goal hiện tại thì giữ nguyên loại (MuscleGain, Endurance…), khác hướng thì lấy loại chuẩn của hướng mới.</summary>
    public static GoalType TypeFor(GoalDirection derived, GoalType current) => derived == DirectionOf(current)
        ? current
        : derived switch { GoalDirection.Loss => GoalType.WeightLoss, GoalDirection.Gain => GoalType.WeightGain, _ => GoalType.Maintenance };

    public static GoalRateBoundsModel Bounds(GoalDirection direction, AlgorithmOptions opt)
    {
        var (slow, standard, fast) = direction switch
        {
            GoalDirection.Loss => (opt.LossRateSlowMaxPercent, opt.LossRateStandardMaxPercent, opt.LossRateFastMaxPercent),
            GoalDirection.Gain => (opt.GainRateSlowMaxPercent, opt.GainRateStandardMaxPercent, opt.GainRateFastMaxPercent),
            _ => (0m, 0m, 0m),
        };
        return new GoalRateBoundsModel(slow, standard, fast, Math.Round((slow + standard) / 2m, 2));
    }

    public static string Band(decimal magnitudePercent, GoalRateBoundsModel b)
        => magnitudePercent <= 0 ? "None" : magnitudePercent <= b.SlowMaxPercent ? "Slow" : magnitudePercent <= b.StandardMaxPercent ? "Standard" : "Fast";

    /// <param name="targetKg">Đích user muốn; null = không đặt đích (giữ cân).</param>
    /// <param name="ratePercent">Tốc độ mong muốn, độ lớn % cân nặng/tuần (hướng do loại goal suy ra); null = giữ tốc độ hiện tại hoặc giá trị mặc định.</param>
    public static GoalEditResult Compute(GoalEditState s, decimal? targetKg, decimal? ratePercent, AlgorithmOptions opt, DateOnly today)
    {
        var current = s.CurrentWeightKg;
        if (targetKg is { } t && Math.Abs(t - current) / current > MaxTargetDeviation)
            throw new BadRequestException(ExceptionMessages.GoalTargetTooFar);

        var derived = Derive(current, targetKg);
        var currentDirection = DirectionOf(s.GoalType);
        var directionChanged = derived != currentDirection;
        var bounds = Bounds(derived, opt);

        decimal magnitude = 0;
        if (derived != GoalDirection.Maintain)
        {
            var keep = currentDirection == derived ? Math.Min(Math.Abs(s.ProgramRatePercent), bounds.FastMaxPercent) : 0m;
            magnitude = ratePercent ?? (keep > 0 ? keep : bounds.DefaultPercent);
            if (magnitude <= 0)
                throw new BadRequestException(ExceptionMessages.GoalRateRequired);
            if (magnitude > bounds.FastMaxPercent)
                throw new BadRequestException(ExceptionMessages.GoalRateTooHigh(bounds.FastMaxPercent));
        }

        var signedPercent = derived == GoalDirection.Gain ? magnitude : -magnitude;
        var weeklyKg = Math.Round(signedPercent / 100m * current, 2);

        int? budget = s.ProgramType != ProgramType.Manual && s.TdeeKcal is { } tdee
            ? MacroTargetCalculator.DailyCalories(tdee, -signedPercent, current, opt.KcalPerKg, s.ProgramType == ProgramType.Coached ? s.FloorKcal : null)
            : null;

        DateOnly? end = derived != GoalDirection.Maintain && targetKg is { } to && weeklyKg != 0
            ? today.AddDays((int)Math.Ceiling(Math.Abs(to - current) / Math.Abs(weeklyKg) * 7m))
            : null;

        var currentSigned = -s.ProgramRatePercent;
        var newType = TypeFor(derived, s.GoalType);
        var preview = new GoalEditPreviewResponse(
            newType.ToString(), s.GoalType.ToString(), current, directionChanged ? today : s.GoalStartDate, targetKg,
            signedPercent, weeklyKg, weeklyKg * 4m, signedPercent * 4m,
            budget, end, bounds, derived == GoalDirection.Maintain ? "None" : Band(magnitude, bounds),
            directionChanged, directionChanged, s.ProgramType == ProgramType.Manual,
            new GoalEditChangesModel(
                new GoalEditChangeModel(s.GoalTargetKg, targetKg, s.GoalTargetKg != targetKg),
                new GoalEditChangeModel(currentSigned, signedPercent, currentSigned != signedPercent)));

        return new GoalEditResult(preview, newType, -signedPercent, weeklyKg);
    }
}
