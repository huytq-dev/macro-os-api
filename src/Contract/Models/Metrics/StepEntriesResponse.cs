namespace Contract;

/// <param name="Last7DayAverage">Trung bình bước/ngày của các ngày có dữ liệu trong 7 ngày cuối khoảng hỏi; null nếu không có.</param>
public sealed record StepEntriesResponse(IReadOnlyList<StepEntryModel> Items, int? Last7DayAverage);
