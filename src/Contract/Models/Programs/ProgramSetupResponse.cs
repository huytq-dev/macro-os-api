namespace Contract;

/// <summary>Mọi số wizard New Program cần để vẽ và chặn: cân nặng, TDEE, sàn kcal, trần kcal/protein, 7 ngày mặc định (duy trì cân, Balanced, protein vừa).</summary>
public sealed record ProgramSetupResponse(
    decimal WeightKg,
    int EstimatedTdeeKcal,
    ProgramFloorsModel Floors,
    int KcalMax,
    decimal ProteinPerKgMax,
    IReadOnlyList<ProgramDayModel> Defaults,
    ProgramSetupGoalModel? Goal,
    string CheckInWeekday
);
