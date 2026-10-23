namespace Application;

/// <summary>
/// Thống kê bước chân của <c>GET /steps</c> (hàm thuần, tách khỏi handler để test; KHÔNG đổi quy tắc). "7 ngày cuối" = 7 ngày LỊCH kết thúc ở <c>to</c> (gồm <c>to</c>): <c>LogDate &gt; to − 7</c>.
/// <c>Last7DayAverage</c> = trung bình bước/ngày của CÁC NGÀY CÓ DỮ LIỆU trong cửa sổ đó (chia cho số ngày có dữ liệu, không chia 7; ngày không nhập không tính là 0); null khi không có ngày nào.
/// Nó KHÔNG phải số của ngày gần nhất hay của hôm nay: chuỗi từng ngày nằm ở <c>Items</c> (mới nhất trước).
/// </summary>
public static class StepSummary
{
    public const int RecentDays = 7;

    public static List<StepEntryModel> Recent(IEnumerable<StepEntryModel> items, DateOnly to) => items.Where(i => i.LogDate > to.AddDays(-RecentDays)).ToList();

    public static int? Last7DayAverage(IEnumerable<StepEntryModel> items, DateOnly to)
    {
        var recent = Recent(items, to);
        return recent.Count == 0 ? null : (int)Math.Round(recent.Average(i => i.Steps));
    }
}
