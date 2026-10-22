namespace Contract;

/// <summary>
/// Nguồn sự thật duy nhất của mọi con số màn Edit Goal. Tốc độ có DẤU: dương = tăng cân, âm = giảm (cùng Goal.RatePerWeekKg).
/// </summary>
/// <param name="GoalType">Loại goal suy ra từ đích so với cân hiện tại (giữ MuscleGain nếu goal hiện là MuscleGain và vẫn cùng hướng tăng).</param>
/// <param name="StartDate">Ngày bắt đầu goal hiện tại; = hôm nay khi <paramref name="RequiresNewProgram"/>.</param>
/// <param name="InitialDailyBudgetKcal">null với program Manual (đặt tay) hoặc khi chưa có TDEE.</param>
/// <param name="ProjectedEndDate">Lạc quan: hôm nay + |đích − cân| ÷ tốc độ/tuần. Khác GoalProgress.EstimatedCompletionDate (ngoại suy từ dữ liệu thật). null khi giữ cân hoặc chưa có đích.</param>
/// <param name="RateBand">Slow | Standard | Fast; None khi giữ cân.</param>
/// <param name="RequiresNewProgram">Đổi hướng goal (Loss ⇄ Gain ⇄ Maintenance): không sửa tại chỗ, FE chuyển sang wizard New Program.</param>
public sealed record GoalEditPreviewResponse(
    string GoalType,
    string CurrentGoalType,
    decimal CurrentWeightKg,
    DateOnly StartDate,
    decimal? TargetWeightKg,
    decimal WeeklyRatePercent,
    decimal WeeklyRateKg,
    decimal MonthlyRateKg,
    decimal MonthlyRatePercent,
    int? InitialDailyBudgetKcal,
    DateOnly? ProjectedEndDate,
    GoalRateBoundsModel RateBounds,
    string RateBand,
    bool GoalTypeChanged,
    bool RequiresNewProgram,
    bool IsManualProgram,
    GoalEditChangesModel Changes,
    FeasibilityResponse? Feasibility = null
);
