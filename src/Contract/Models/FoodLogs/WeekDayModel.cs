namespace Contract;

/// <param name="IsLogged">Ngày đã log: có entry, manual override hoặc nhịn ăn.</param>
/// <param name="Totals">Tổng RAW của các món Food Log (nhịn ăn = 0).</param>
/// <param name="Effective">Dinh dưỡng hiệu lực của ngày (Manual → Food Log → Fasting); null khi ngày chưa log. Dùng cái này cho số "ăn trong ngày".</param>
public sealed record WeekDayModel(DateOnly LogDate, bool IsLogged, MacroTotals Totals, ProgramWeekModel? Target, EffectiveNutritionModel? Effective = null);
