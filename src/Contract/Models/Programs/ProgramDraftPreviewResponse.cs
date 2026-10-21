namespace Contract;

/// <summary>Xem trước program chưa tạo (không ghi DB). Rate/kg là SUY RA từ kcal trung bình so với TDEE: dương = tăng cân, âm = giảm (cùng quy ước onboarding preview).</summary>
public sealed record ProgramDraftPreviewResponse(
    IReadOnlyList<ProgramDayModel> Days,
    int AverageCalories,
    int WeeklyBudgetKcal,
    decimal AverageProteinG,
    decimal AverageCarbsG,
    decimal AverageFatG,
    int MacroKcalAverage,
    int EstimatedTdeeKcal,
    decimal WeeklyChangeKg,
    decimal WeeklyChangePercent,
    DateOnly? ProjectedEndDate,
    IReadOnlyList<ProgramDraftWarning> Warnings
);
