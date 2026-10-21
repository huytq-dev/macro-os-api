namespace Contract;

/// <param name="Target">Target của MacroProgramWeek hiệu lực tại ngày này; null nếu chưa có program.</param>
/// <param name="Notes">Ghi chú của ngày, theo giờ rồi theo thời điểm tạo (không còn là chuỗi như trước S10).</param>
/// <param name="IsFasting">Ngày đánh dấu nhịn ăn (không có entry): IsLogged = true với 0 kcal.</param>
/// <param name="IsIncomplete">Ngày đánh dấu "log chưa đủ": expenditure bỏ qua ngày này.</param>
/// <param name="NotesMaxPerDay">Số ghi chú tối đa mỗi ngày; thêm ghi chú thứ 6 trả 409.</param>
/// <param name="WaterMl">Tổng nước uống ghi trong ngày (ml), xem GET /food-logs/water.</param>
public sealed record DayLogResponse(
    DateOnly LogDate,
    bool IsLogged,
    IReadOnlyList<FoodLogNoteModel> Notes,
    MacroTotals Totals,
    ProgramWeekModel? Target,
    IReadOnlyList<FoodLogEntryModel> Entries,
    decimal WaterMl,
    int NotesMaxPerDay = FoodLogNoteLimits.MaxPerDay,
    bool IsFasting = false,
    bool IsIncomplete = false
);
