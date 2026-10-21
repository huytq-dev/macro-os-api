namespace Application;

public static class WeighInStreak
{
    public static WeighInStreaks Compute(IEnumerable<DateOnly> weighInDates, DateOnly today)
    {
        var days = weighInDates.Where(d => d <= today).Distinct().Order().ToList();
        if (days.Count == 0) return new WeighInStreaks(0, 0, false);

        // Chuỗi dài nhất: quét một lượt các ngày đã sắp xếp
        int longest = 1, run = 1;
        for (var i = 1; i < days.Count; i++)
        {
            run = days[i].DayNumber == days[i - 1].DayNumber + 1 ? run + 1 : 1;
            longest = Math.Max(longest, run);
        }

        // Chuỗi hiện tại: lùi từ hôm nay (hoặc hôm qua nếu hôm nay chưa cân)
        var set = days.ToHashSet();
        var includesToday = set.Contains(today);
        var cursor = includesToday ? today : today.AddDays(-1);
        var current = 0;
        while (set.Contains(cursor)) { current++; cursor = cursor.AddDays(-1); }
        return new WeighInStreaks(current, longest, includesToday);
    }
}
