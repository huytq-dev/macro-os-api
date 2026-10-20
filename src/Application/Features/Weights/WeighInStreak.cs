namespace Application;

/// <summary>Chuỗi ngày cân liên tiếp (thuần, không DB).</summary>
/// <param name="Current">Số ngày liên tiếp kết thúc hôm nay; hôm nay chưa cân thì tính đến hôm qua (chưa đứt chuỗi cho tới hết hôm nay); 0 nếu cả hôm nay lẫn hôm qua đều không cân.</param>
/// <param name="Longest">Chuỗi dài nhất từ trước tới nay.</param>
/// <param name="IncludesToday">true = hôm nay đã cân (chuỗi hiện tại đã gồm hôm nay); false = chuỗi hiện tại (nếu &gt; 0) sẽ đứt nếu hôm nay không cân.</param>
public readonly record struct WeighInStreaks(int Current, int Longest, bool IncludesToday);

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
