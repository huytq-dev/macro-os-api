namespace Application;

/// <summary>Ghi/xoá từng chỉ số của MỘT ngày trên hai hàng của BodyMetric (Visual và Tape) trong cùng unit of work; dùng chung cho Full Body và các trang từng số đo.</summary>
internal static class FullBodyWriter
{
    /// <param name="changes">khoá → giá trị: có giá trị = ghi/ghi đè, null = xoá chỉ số đó. Khoá là số đo vòng hoặc <see cref="BodyMeasurements.VisualBodyFatKey"/>.</param>
    /// <returns>Hai hàng sau khi áp (null = không còn) và Changed = có ghi mới hoặc xoá được một giá trị đang có. Chưa lưu.</returns>
    public static async Task<(BodyMetric? Visual, BodyMetric? Tape, bool Changed)> ApplyAsync(
        IUnitOfWork uow, Guid userId, DateOnly date, IReadOnlyDictionary<string, decimal?> changes, CancellationToken ct)
    {
        var repo = uow.Repository<BodyMetric>();
        // MỌI hàng Visual/Tape của ngày (DB không chặn trùng ngày), mới nhất trước: ghi vào hàng mới nhất (cùng quy tắc ĐỌC), xoá ở mọi hàng — xem TapeDayRows
        var dayRows = await repo.Query(tracking: true)
            .Where(b => b.UserId == userId && b.MeasuredDate == date && (b.Method == MeasurementMethod.Visual || b.Method == MeasurementMethod.Tape))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
        var visualRows = dayRows.Where(r => r.Method == MeasurementMethod.Visual).ToList();
        var tapeRows = dayRows.Where(r => r.Method == MeasurementMethod.Tape).ToList();
        var changed = false;

        foreach (var (key, value) in changes)
        {
            if (string.Equals(key, BodyMeasurements.VisualBodyFatKey, StringComparison.OrdinalIgnoreCase))
            {
                if (value is { } percent)
                {
                    var visual = visualRows.FirstOrDefault();
                    if (visual is null)
                    {
                        visual = new BodyMetric { Id = Guid.NewGuid(), UserId = userId, MeasuredDate = date, Method = MeasurementMethod.Visual };
                        await repo.AddAsync(visual, ct);
                        visualRows.Add(visual);
                        changed = true;
                    }
                    if (visual.BodyFatPercent != percent) { visual.BodyFatPercent = percent; changed = true; }
                }
                else if (visualRows.Count > 0)
                {
                    foreach (var row in visualRows) await repo.RemoveAsync(row, ct); // interceptor chuyển thành soft delete (mọi hàng trùng ngày)
                    visualRows.Clear();
                    changed = true;
                }
                continue;
            }

            var measurement = BodyMeasurements.Find(key)!;
            var (rowChanged, added) = TapeDayRows.Apply(tapeRows, measurement, value, () => new BodyMetric { Id = Guid.NewGuid(), UserId = userId, MeasuredDate = date, Method = MeasurementMethod.Tape });
            foreach (var row in added) await repo.AddAsync(row, ct);
            changed |= rowChanged;
        }

        // Hàng Tape không còn ô nào thì bỏ hẳn (ngày đó không còn số đo vòng ở hàng đó)
        foreach (var row in TapeDayRows.Emptied(tapeRows))
        {
            await repo.RemoveAsync(row, ct);
            tapeRows.Remove(row);
        }
        return (visualRows.FirstOrDefault(), tapeRows.FirstOrDefault(), changed);
    }

    /// <summary>Cân và trend của các ngày trong khoảng (nếu có), để đính kèm vào Full Body / so sánh.</summary>
    public static async Task<Dictionary<DateOnly, (decimal Scale, decimal? Trend)>> WeightsAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
        => (await uow.Repository<WeightEntry>().Query()
                .Where(w => w.UserId == userId && w.LogDate >= from && w.LogDate <= to)
                .Select(w => new { w.LogDate, w.WeightKg, w.TrendWeightKg })
                .ToListAsync(ct))
            .ToDictionary(w => w.LogDate, w => (w.WeightKg, w.TrendWeightKg));
}
