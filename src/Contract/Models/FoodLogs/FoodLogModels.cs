namespace Contract;

public sealed record MacroTotals(decimal Calories, decimal Protein, decimal Carbs, decimal Fat, decimal Fiber);

/// <param name="Quantity">Số serving nếu có FoodServingId, ngược lại là số gram.</param>
public sealed record FoodLogEntryModel(
    Guid Id,
    string MealType,
    TimeOnly? EatenAt,
    Guid? FoodItemId,
    Guid? RecipeId,
    string FoodName,
    Guid? FoodServingId,
    string? ServingName,
    decimal Quantity,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal? Fiber
);

/// <param name="Target">Target của MacroProgramWeek hiệu lực tại ngày này; null nếu chưa có program.</param>
public sealed record DayLogResponse(
    DateOnly LogDate,
    string? Notes,
    MacroTotals Totals,
    ProgramWeekModel? Target,
    IReadOnlyList<FoodLogEntryModel> Entries
);

/// <param name="Target">Target của MacroProgramWeek hiệu lực tại ngày này; null nếu chưa có program.</param>
public sealed record WeekDayModel(DateOnly LogDate, MacroTotals Totals, ProgramWeekModel? Target);

/// <summary>7 ngày Thứ 2 → Chủ nhật của tuần được hỏi.</summary>
public sealed record WeekLogResponse(IReadOnlyList<WeekDayModel> Days);
