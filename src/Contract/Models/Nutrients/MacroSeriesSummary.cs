namespace Contract;

/// <param name="LoggedDays">Số ngày đã log hợp lệ trong [From, To] (mẫu số của mọi trung bình).</param>
/// <param name="CalendarDays">Số ngày lịch của khoảng, chỉ để tham khảo (KHÔNG dùng làm mẫu số).</param>
public sealed record MacroSeriesSummary(DateOnly From, DateOnly To, int LoggedDays, int CalendarDays, MacroAverages Averages);
