namespace Application;

public sealed class GetWeightEntriesQueryHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    TimeProvider _timeProvider
) : IRequestHandler<GetWeightEntriesQuery, WeightEntriesResponse>
{
    public async Task<WeightEntriesResponse> Handle(GetWeightEntriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To ?? locale.Today;
        var from = request.From ?? (request.All == true ? DateOnly.MinValue : to.AddDays(-90));

        var items = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate >= from && w.LogDate <= to)
            .OrderByDescending(w => w.LogDate)
            .ProjectToType<WeightEntryModel>()
            .ToListAsync(ct);

        if (request.From is null && request.All == true) from = items.Count == 0 ? to : items[^1].LogDate;

        // % mỡ đo bằng cân của từng ngày (một truy vấn cho cả khoảng); mỗi ngày lấy lần ghi mới nhất
        if (items.Count > 0)
        {
            var bodyFat = (await _uow.Repository<BodyMetric>().Query()
                    .Where(b => b.UserId == userId && (b.Method == MeasurementMethod.Scale || b.Method == MeasurementMethod.SmartScale)
                        && b.BodyFatPercent != null && b.MeasuredDate >= from && b.MeasuredDate <= to)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new { b.MeasuredDate, Percent = b.BodyFatPercent!.Value })
                    .ToListAsync(ct))
                .GroupBy(b => b.MeasuredDate).ToDictionary(g => g.Key, g => g.First().Percent);
            if (bodyFat.Count > 0)
                items = items.Select(i => bodyFat.TryGetValue(i.LogDate, out var pct) ? i with { BodyFatPercent = pct } : i).ToList();
        }

        var latest = items.FirstOrDefault();
        var weekAgo = latest is null ? null
            : items.FirstOrDefault(i => i.TrendWeightKg != null && i.LogDate <= latest.LogDate.AddDays(-7));

        // Cùng cổng dữ liệu với Weight Trend Insights (≥ 3 ngày cân thật trong 20 ngày và ≥ 1 trong 7 ngày, ngày local): không đủ ⇒ rate null + mã lý do, không bao giờ 0
        // (gồm lần cân ngay TRƯỚC cửa sổ để phát hiện khoảng trống dài cắt ngang biên: guard G = 14 ngày; xem WeightRateGateSupport)
        var gate = await WeightRateGateSupport.LoadAsync(_uow, userId, to, ct);
        var rate = gate.Reason is null && latest?.TrendWeightKg is { } lt && weekAgo?.TrendWeightKg is { } wt
            ? WeightTrendCalculator.RatePerWeek(weekAgo.LogDate, wt, latest.LogDate, lt)
            : null;
        var rateReason = gate.Reason ?? (rate is null ? WeightTrendSeries.InsufficientWeightData : null);

        var (summary, buckets) = MetricSeries.Build(items.Select(i => (i.LogDate, i.WeightKg)), from, to, request.Granularity, locale.WeekStart);
        var shown = request.Limit is { } limit ? items.Take(Math.Clamp(limit, 1, 1000)).ToList() : items;
        return new WeightEntriesResponse(shown, latest?.WeightKg, latest?.TrendWeightKg, rate, summary, buckets, rateReason);
    }
}
