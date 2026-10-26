namespace Application;

/// <summary>
/// MỘT điểm tính Flux Range cho mọi nơi hiển thị (series, bucket Week/Month, GET /expenditure, insights): chạy <see cref="ExpenditureFluxCalculator"/> với chính sách <see cref="FluxCalculationPolicy.V1"/> trên CHÍNH quan sát và chuỗi TDEE đã học
/// của production (không ghi gì, không đổi TDEE/status/check-in). Tính khi ĐỌC từ log + cân + snapshot đã lưu (không cần cột mới, không migration), nhân quả và xác định: ngày d chỉ dùng dữ liệu ≤ d nên sửa dữ liệu cũ tự sinh lại dải đúng.
/// Dải = bao min/max của các quỹ đạo ứng viên (độ nhạy chậm … nhanh) VÀ TDEE đã học: LUÔN chứa TDEE đã học; null khi Calibrating hoặc chưa từng có quan sát hợp lệ (không bao giờ dải giả). Đây là "ước lượng bất đồng giữa các độ nhạy" của MacroOS,
/// KHÔNG phải khoảng tin cậy và không khẳng định giống MacroFactor (MF chỉ công bố định tính, xem Docs/Design/Flux).
/// </summary>
public static class ExpenditureFluxService
{
    /// <summary>Hàm thuần: dải theo ngày từ chuỗi đã học (<paramref name="raw"/>, liên tục tới hôm nay) và quan sát từng ngày. <paramref name="start"/> null = ngày đầu của chuỗi.</summary>
    public static IReadOnlyDictionary<DateOnly, FluxPoint> Bands(IReadOnlyList<ExpenditureSeriesPoint> raw, IReadOnlyList<ExpenditureDay> observations, DateOnly? start, int kcalPerKg)
    {
        if (raw.Count == 0) return new Dictionary<DateOnly, FluxPoint>();
        var learned = raw.Select(p => new FluxLearnedDay(p.Date, p.Tdee, Enum.TryParse<ExpenditureStatus>(p.Status, out var s) ? s : ExpenditureStatus.Holding)).ToList();
        return ExpenditureFluxCalculator.Compute(observations, learned, start ?? raw[0].Date, FluxCalculationPolicy.V1, kcalPerKg).ToDictionary(p => p.Date);
    }

    /// <summary>Nạp quan sát (log hợp lệ + trend cân tại ngày cân, đúng như rebuilder) rồi tính dải cho toàn chuỗi <paramref name="raw"/>.</summary>
    public static async Task<IReadOnlyDictionary<DateOnly, FluxPoint>> LoadBandsAsync(IUnitOfWork uow, Guid userId, IReadOnlyList<ExpenditureSeriesPoint> raw, DateOnly? start, AlgorithmOptions opt, CancellationToken ct)
    {
        // Flux đang ẨN theo quyết định user (AlgorithmOptions.FluxRangeEnabled = false mặc định): không tính, mọi dải = null
        if (!opt.FluxRangeEnabled || raw.Count == 0) return new Dictionary<DateOnly, FluxPoint>();
        var first = raw[0].Date; var last = raw[^1].Date;
        var from = start is { } s && s < first ? s : first;
        var logs = await FoodLogSupport.LoadDaysAsync(uow, userId, from, last, ct, includeFasting: true);
        var trends = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate >= from && w.LogDate <= last && w.TrendWeightKg != null)
            .Select(w => new { w.LogDate, Trend = w.TrendWeightKg!.Value }).ToDictionaryAsync(w => w.LogDate, w => w.Trend, ct);
        return Bands(raw, ExpenditureObservationResolver.Daily(from, last, logs, trends), start, opt.KcalPerKg);
    }

    /// <summary>Phiên bản chính sách khi có ít nhất một dải; null nếu không.</summary>
    public static string? Model(IEnumerable<FluxPoint> points) => points.Any(p => p.LowerKcal is not null) ? FluxCalculationPolicy.V1Version : null;

    /// <summary>
    /// Chính sách gộp dải theo bucket (BE định nghĩa, MF không công bố): dải của bucket = trung bình kcal hiển thị của bucket ± NỬA ĐỘ RỘNG trung bình của các ngày CÓ DẢI trong bucket
    /// (<c>Low = ⌊Average − mean(kcal − Lower)⌋</c>, <c>High = ⌈Average + mean(Upper − kcal)⌉</c>). Bám vào Average nên luôn <c>Low ≤ Average ≤ High</c> kể cả khi bucket có cả ngày Calibrating (không dải) lẫn Updating;
    /// bề rộng cùng cỡ dải ngày (không phình theo độ dài bucket như min/max); với bucket mọi ngày đều có dải thì bằng trung bình Lower / Upper hằng ngày. Bucket không ngày nào có dải ⇒ null.
    /// </summary>
    public static List<ExpenditureBucketModel> Buckets(IReadOnlyList<MetricBucket> buckets, IReadOnlyList<ExpenditureSeriesDay> days, MetricGranularity? granularity, DayOfWeek weekStart)
    {
        if (granularity is not { } g) return [];
        var byBucket = days.Where(d => d.FluxLowKcal is not null && d.FluxHighKcal is not null)
            .GroupBy(d => MetricSeries.BucketStart(d.Date, g, weekStart)).ToDictionary(x => x.Key);
        return buckets.Select(b => byBucket.TryGetValue(b.Start, out var inBucket)
            ? new ExpenditureBucketModel(b.Start, b.End, b.Count, b.Average,
                Math.Floor(b.Average - inBucket.Average(d => d.ExpenditureKcal - d.FluxLowKcal!.Value)), Math.Ceiling(b.Average + inBucket.Average(d => d.FluxHighKcal!.Value - d.ExpenditureKcal)))
            : new ExpenditureBucketModel(b.Start, b.End, b.Count, b.Average, null, null)).ToList();
    }
}
