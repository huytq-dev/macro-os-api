namespace Contract;

/// <summary>
/// Một vi phạm khả thi của target (tuần hoặc một ngày). <c>Code</c> là mã lý do cấp cao (INVALID_CALORIE_TARGET | MACRO_BUDGET_EXCEEDED | INFEASIBLE_PROGRAM),
/// <c>Detail</c> là nguyên nhân cụ thể (NON_POSITIVE_CALORIES | BELOW_FLOOR | MACRO_EXCEEDS_CALORIES | NEGATIVE_MACRO | WEEKLY_BUDGET_NOT_CONSERVED).
/// </summary>
/// <param name="Day">Thứ (Monday..Sunday) hoặc null khi vi phạm ở target tuần.</param>
/// <param name="MacroKcal">4·P + 4·C + 9·F của target đó (làm tròn).</param>
/// <param name="MinFeasibleCalories">kcal tối thiểu để đủ macro hiện tại (chỉ khi macro vượt kcal), ngược lại null.</param>
/// <param name="FloorKcal">Sàn kcal đang áp dụng; null = không có sàn.</param>
public sealed record FeasibilityViolationModel(
    string Code,
    string Detail,
    string? Day,
    int Calories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    int MacroKcal,
    int? MinFeasibleCalories,
    int? FloorKcal);
