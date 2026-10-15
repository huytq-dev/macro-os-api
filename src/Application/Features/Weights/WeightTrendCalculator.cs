namespace Application;

/// <summary>ALG-02: trend cân nặng = EMA(N ngày) trên chuỗi ngày liên tục; ngày thiếu nội suy tuyến tính (BR-10). Hàm thuần.</summary>
public static class WeightTrendCalculator
{
    /// <summary>Trả trend cho đúng các ngày có cân (input không cần liên tục, không cần sắp).</summary>
    public static Dictionary<DateOnly, decimal> Compute(IEnumerable<(DateOnly Date, decimal Kg)> entries, int trendDays)
    {
        var points = entries.OrderBy(e => e.Date).ToList();
        var result = new Dictionary<DateOnly, decimal>();
        if (points.Count == 0) return result;

        var alpha = 2m / (trendDays + 1);
        var trend = points[0].Kg;
        result[points[0].Date] = Math.Round(trend, 2);

        for (var i = 1; i < points.Count; i++)
        {
            var (prevDate, prevKg) = points[i - 1];
            var (curDate, curKg) = points[i];
            var gap = curDate.DayNumber - prevDate.DayNumber;

            // Nội suy từng ngày trong khoảng trống rồi cho EMA chạy qua
            for (var d = 1; d <= gap; d++)
            {
                var kg = prevKg + (curKg - prevKg) * d / gap;
                trend = alpha * kg + (1 - alpha) * trend;
            }
            result[curDate] = Math.Round(trend, 2);
        }

        return result;
    }

    /// <summary>Tốc độ thay đổi trend (kg/tuần) giữa hai mốc; null nếu thiếu dữ liệu.</summary>
    public static decimal? RatePerWeek(DateOnly fromDate, decimal fromTrend, DateOnly toDate, decimal toTrend)
    {
        var days = toDate.DayNumber - fromDate.DayNumber;
        return days <= 0 ? null : Math.Round((toTrend - fromTrend) / days * 7m, 3);
    }
}
