namespace Contract;

public sealed record MacroTotals(decimal Calories, decimal Protein, decimal Carbs, decimal Fat, decimal Fiber);

/// <param name="Quantity">Số serving nếu có FoodServingId, ngược lại là số gram.</param>
/// <param name="Emoji">Emoji đại diện món suy từ nhóm món; null cho nhóm Other, recipe, quick add (FE dùng 🍽️).</param>
/// <param name="WeightGrams">Tổng khối lượng dòng log (gram): số serving × gram của serving, log theo gram thì = quantity; null cho recipe và quick add.</param>
/// <param name="EatenAt">Giờ ăn; luôn có giá trị trong GET /food-logs (dòng cũ không có giờ lấy giờ mặc định của bữa).</param>
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
    decimal? Fiber,
    string? Emoji,
    decimal? WeightGrams,
    string? IconKey = null
);

/// <param name="NotedAt">Giờ gắn với ghi chú; luôn có giá trị với ghi chú tạo qua API.</param>
public sealed record FoodLogNoteModel(Guid Id, string Text, TimeOnly? NotedAt, DateTimeOffset CreatedAt);

/// <param name="Target">Target của MacroProgramWeek hiệu lực tại ngày này; null nếu chưa có program.</param>
/// <param name="Notes">Ghi chú của ngày, theo giờ rồi theo thời điểm tạo (không còn là chuỗi như trước S10).</param>
/// <param name="WaterMl">Tổng nước uống ghi trong ngày (ml), xem GET /food-logs/water.</param>
public sealed record DayLogResponse(
    DateOnly LogDate,
    bool IsLogged,
    IReadOnlyList<FoodLogNoteModel> Notes,
    MacroTotals Totals,
    ProgramWeekModel? Target,
    IReadOnlyList<FoodLogEntryModel> Entries,
    decimal WaterMl
);

/// <param name="Target">Target của MacroProgramWeek hiệu lực tại ngày này; null nếu chưa có program.</param>
/// <param name="IsLogged">Ngày có ≥ 1 món chưa xoá; ngày không có món = false.</param>
public sealed record WeekDayModel(DateOnly LogDate, bool IsLogged, MacroTotals Totals, ProgramWeekModel? Target);

/// <summary>7 ngày Thứ 2 → Chủ nhật của tuần được hỏi.</summary>
public sealed record WeekLogResponse(IReadOnlyList<WeekDayModel> Days);
