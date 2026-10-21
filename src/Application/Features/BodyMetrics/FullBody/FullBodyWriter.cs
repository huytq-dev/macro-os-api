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
        var (visual, tape) = await FullBodySupport.LoadAsync(uow, userId, date, tracking: true, ct);
        var changed = false;

        foreach (var (key, value) in changes)
        {
            if (string.Equals(key, BodyMeasurements.VisualBodyFatKey, StringComparison.OrdinalIgnoreCase))
            {
                if (value is { } percent)
                {
                    if (visual is null)
                    {
                        visual = new BodyMetric { Id = Guid.NewGuid(), UserId = userId, MeasuredDate = date, Method = MeasurementMethod.Visual };
                        await repo.AddAsync(visual, ct);
                        changed = true;
                    }
                    if (visual.BodyFatPercent != percent) { visual.BodyFatPercent = percent; changed = true; }
                }
                else if (visual is not null)
                {
                    await repo.RemoveAsync(visual, ct); // interceptor chuyển thành soft delete
                    visual = null;
                    changed = true;
                }
                continue;
            }

            var measurement = BodyMeasurements.Find(key)!;
            if (value is { } cm)
            {
                if (tape is null)
                {
                    tape = new BodyMetric { Id = Guid.NewGuid(), UserId = userId, MeasuredDate = date, Method = MeasurementMethod.Tape };
                    await repo.AddAsync(tape, ct);
                    changed = true;
                }
                if (measurement.Get(tape) != cm) { measurement.Set(tape, cm); changed = true; }
            }
            else if (tape is not null && measurement.Get(tape) is not null)
            {
                measurement.Set(tape, null);
                changed = true;
            }
        }

        // Hàng Tape không còn ô nào thì bỏ hẳn (ngày đó không còn số đo vòng)
        if (tape is not null && !BodyMeasurements.HasAny(tape))
        {
            await repo.RemoveAsync(tape, ct);
            tape = null;
        }
        return (visual, tape, changed);
    }

    /// <summary>Cân và trend của các ngày trong khoảng (nếu có), để đính kèm vào Full Body / so sánh.</summary>
    public static async Task<Dictionary<DateOnly, (decimal Scale, decimal? Trend)>> WeightsAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
        => (await uow.Repository<WeightEntry>().Query()
                .Where(w => w.UserId == userId && w.LogDate >= from && w.LogDate <= to)
                .Select(w => new { w.LogDate, w.WeightKg, w.TrendWeightKg })
                .ToListAsync(ct))
            .ToDictionary(w => w.LogDate, w => (w.WeightKg, w.TrendWeightKg));
}
