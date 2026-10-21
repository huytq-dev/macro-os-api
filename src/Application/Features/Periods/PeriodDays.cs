namespace Application;

/// <summary>Một kỳ kinh (khoảng ngày liên tục). End null = đang trong kỳ (tính tới hôm nay).</summary>
public sealed record PeriodRange(Guid Id, DateOnly Start, DateOnly? End, string? Symptoms, string? Notes);

/// <param name="Ranges">Danh sách các kỳ SAU khi sửa (giữ nguyên Id của kỳ còn lại; kỳ mới do tách có Id mới).</param>
/// <param name="Changed">false = không có gì đổi (bật ngày đã được theo dõi, tắt ngày chưa theo dõi).</param>
public sealed record PeriodEdit(IReadOnlyList<PeriodRange> Ranges, bool Changed);

/// <summary>
/// Theo dõi kinh THEO NGÀY (như MacroFactor) trên nền lưu theo khoảng (PeriodLog Start–End): bật/tắt một ngày thành mở rộng / gộp / thu hẹp / tách / xoá / tạo kỳ, và trải các kỳ thành danh sách ngày. Hàm thuần.
/// </summary>
public static class PeriodDays
{
    public const int NotesMaxLength = 500;

    public static DateOnly EffectiveEnd(PeriodRange r, DateOnly today) => r.End ?? today;

    /// <summary>Số ngày của kỳ gồm cả hai đầu (kỳ đang diễn ra tính tới hôm nay).</summary>
    public static int LengthDays(PeriodRange r, DateOnly today) => EffectiveEnd(r, today).DayNumber - r.Start.DayNumber + 1;

    public static bool Tracks(PeriodRange r, DateOnly date, DateOnly today) => date >= r.Start && date <= EffectiveEnd(r, today);

    /// <summary>Các ngày đã theo dõi trong [from, to] (không vượt hôm nay), tăng dần, không trùng.</summary>
    public static List<DateOnly> Expand(IEnumerable<PeriodRange> ranges, DateOnly from, DateOnly to, DateOnly today)
    {
        var days = new SortedSet<DateOnly>();
        foreach (var r in ranges)
        {
            var start = r.Start < from ? from : r.Start;
            var end = EffectiveEnd(r, today);
            if (end > to) end = to;
            for (var d = start; d <= end; d = d.AddDays(1)) days.Add(d);
        }
        return [.. days];
    }

    /// <summary>
    /// Bật (<paramref name="on"/> = true) hoặc tắt một ngày. Bật: đã theo dõi → không đổi; sát cuối kỳ A (date = end + 1) → kéo dài A; sát đầu kỳ B (date = start − 1) → kéo B lùi; lấp đúng khe 1 ngày giữa A và B → gộp
    /// (giữ triệu chứng/ghi chú của kỳ sớm, nối ghi chú kỳ sau, bỏ kỳ sau); còn lại tạo kỳ [date, date]. Tắt: kỳ 1 ngày → xoá; ở đầu → thu hẹp đầu; ở cuối → thu hẹp cuối;
    /// ở giữa → tách hai (kỳ sau có Id mới, sao triệu chứng; kỳ đang diễn ra thì kỳ sau vẫn đang diễn ra); ngày chưa theo dõi → không đổi.
    /// </summary>
    public static PeriodEdit SetDay(IReadOnlyList<PeriodRange> ranges, DateOnly date, bool on, DateOnly today)
    {
        var list = ranges.OrderBy(r => r.Start).ToList();
        var inside = list.FirstOrDefault(r => Tracks(r, date, today));

        if (on)
        {
            if (inside is not null) return new PeriodEdit(list, false);
            var before = list.FirstOrDefault(r => EffectiveEnd(r, today) == date.AddDays(-1));
            var after = list.FirstOrDefault(r => r.Start == date.AddDays(1));
            if (before is not null && after is not null)
            {
                list[list.IndexOf(before)] = before with { End = after.End, Notes = JoinNotes(before.Notes, after.Notes) };
                list.Remove(after);
            }
            else if (before is not null) list[list.IndexOf(before)] = before with { End = date };
            else if (after is not null) list[list.IndexOf(after)] = after with { Start = date };
            else list.Add(new PeriodRange(Guid.NewGuid(), date, date, null, null));
            return new PeriodEdit([.. list.OrderBy(r => r.Start)], true);
        }

        if (inside is null) return new PeriodEdit(list, false);
        var end = EffectiveEnd(inside, today);
        var i = list.IndexOf(inside);
        if (inside.Start == date && end == date) list.RemoveAt(i);
        else if (inside.Start == date) list[i] = inside with { Start = date.AddDays(1) };
        else if (end == date) list[i] = inside with { End = date.AddDays(-1) };
        else
        {
            list[i] = inside with { End = date.AddDays(-1) };
            list.Add(new PeriodRange(Guid.NewGuid(), date.AddDays(1), inside.End, inside.Symptoms, null));
        }
        return new PeriodEdit([.. list.OrderBy(r => r.Start)], true);
    }

    private static string? JoinNotes(string? earlier, string? later)
    {
        var joined = string.Join("\n", new[] { earlier, later }.Where(n => !string.IsNullOrWhiteSpace(n)));
        if (joined.Length == 0) return null;
        return joined.Length > NotesMaxLength ? joined[..NotesMaxLength] : joined;
    }
}
