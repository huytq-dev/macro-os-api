namespace Application;

public static class PeriodSupport
{
    /// <summary>Tính lại cả chuỗi: CycleLengthDays = khoảng cách tới StartDate kỳ trước (kỳ đầu null); PeriodLengthDays = số ngày gồm cả hai đầu (đang trong kỳ = null).</summary>
    public static void Recompute(IEnumerable<PeriodLog> logs)
    {
        PeriodLog? previous = null;
        foreach (var log in logs.OrderBy(l => l.StartDate))
        {
            log.CycleLengthDays = previous is null ? null : log.StartDate.DayNumber - previous.StartDate.DayNumber;
            log.PeriodLengthDays = log.EndDate is { } end ? end.DayNumber - log.StartDate.DayNumber + 1 : null;
            previous = log;
        }
    }
}
