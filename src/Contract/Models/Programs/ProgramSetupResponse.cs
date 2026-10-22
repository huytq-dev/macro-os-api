namespace Contract;

/// <summary>
/// Mọi số wizard New Program cần để vẽ và chặn: cân nặng, TDEE, sàn kcal, trần kcal/protein, 7 ngày mặc định (duy trì cân, Balanced, protein vừa).
/// <c>TdeeSource</c> = LearnedUpdating | LearnedHolding | ManualInitial | Formula (cùng resolver với preview/create); <c>TdeeStale</c> = đang dựng lại dữ liệu dẫn xuất (xem /derived-data/status), số TDEE có thể chưa khớp dữ liệu mới nhất.
/// </summary>
public sealed record ProgramSetupResponse(
    decimal WeightKg,
    int EstimatedTdeeKcal,
    ProgramFloorsModel Floors,
    int KcalMax,
    decimal ProteinPerKgMax,
    IReadOnlyList<ProgramDayModel> Defaults,
    ProgramSetupGoalModel? Goal,
    string CheckInWeekday,
    string TdeeSource = "Formula",
    bool TdeeStale = false
);
