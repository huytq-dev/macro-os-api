namespace Application;

/// <summary>Một ngày của chuỗi trend liên tục ở độ chính xác đầy đủ (chưa làm tròn).</summary>
public readonly record struct TrendPoint(DateOnly Date, decimal Trend, decimal? Scale);

/// <summary>
/// Trend cân nặng MỖI NGÀY (không chỉ ngày có cân) + Insights của trang Weight Trend. Hàm thuần, nguồn dữ liệu duy nhất là các lần cân (WeightEntry).
/// Cùng thuật toán với <see cref="WeightTrendCalculator.Compute"/> (ALG-02): EMA với α = 2 / (N + 1), ngày giữa hai lần cân dùng giá trị NỘI SUY TUYẾN TÍNH
/// (MacroFactor: "missing days are filled by linear interpolation", trung bình nghiêng về gần đây; help.macrofactorapp.com/dashboard/weight_trend).
/// Khác ở chỗ: trả trend cho TỪNG ngày từ lần cân đầu tiên đến ngày kết thúc (hôm nay). Sau lần cân cuối, EMA vẫn chạy với giá trị cân cuối
/// (cùng quy tắc nội suy "giữ giá trị gần nhất": trend tiến dần về số cân cuối nếu không cân thêm).
/// </summary>
public static class WeightTrendSeries
{
    /// <param name="entries">Mỗi ngày tối đa một lần cân (trùng ngày lấy dòng cuối); ngày sau <paramref name="endDate"/> bị bỏ; không cần sắp xếp.</param>
    /// <param name="trendDays">N của EMA (α = 2 / (N + 1)).</param>
    /// <returns>Mỗi ngày liên tục từ ngày cân đầu tiên đến <paramref name="endDate"/>; rỗng nếu chưa có cân.</returns>
    public static List<TrendPoint> Daily(IEnumerable<(DateOnly Date, decimal Kg)> entries, int trendDays, DateOnly endDate)
    {
        var points = entries.Where(e => e.Date <= endDate).GroupBy(e => e.Date).Select(g => g.Last()).OrderBy(e => e.Date).ToList();
        var result = new List<TrendPoint>();
        if (points.Count == 0) return result;

        var alpha = 2m / (trendDays + 1);
        var next = 0;           // chỉ số điểm cân đầu tiên có ngày >= d
        decimal trend = 0;
        for (var d = points[0].Date; d <= endDate; d = d.AddDays(1))
        {
            while (next < points.Count && points[next].Date < d) next++;

            decimal target;
            decimal? scale = null;
            if (next < points.Count && points[next].Date == d) { target = points[next].Kg; scale = target; }
            else if (next < points.Count)
            {
                var prev = points[next - 1];
                var nxt = points[next];
                target = prev.Kg + (nxt.Kg - prev.Kg) * (d.DayNumber - prev.Date.DayNumber) / (nxt.Date.DayNumber - prev.Date.DayNumber);
            }
            else target = points[^1].Kg; // sau lần cân cuối: giữ cân cuối

            trend = result.Count == 0 ? target : alpha * target + (1 - alpha) * trend;
            result.Add(new TrendPoint(d, trend, scale));
        }
        return result;
    }

    /// <summary>Làm tròn 2 chữ số và tính thay đổi so với hôm trước (từ giá trị chưa làm tròn).</summary>
    public static List<WeightTrendDay> ToDays(IReadOnlyList<TrendPoint> raw)
    {
        var days = new List<WeightTrendDay>(raw.Count);
        for (var i = 0; i < raw.Count; i++)
            days.Add(new WeightTrendDay(raw[i].Date, Round(raw[i].Trend), raw[i].Scale is { } s ? Round(s) : null,
                i == 0 ? null : Round(raw[i].Trend - raw[i - 1].Trend)));
        return days;
    }

    public static readonly int[] ChangeWindows = [3, 7, 14, 30, 90];
    public const int RegressionDays = 21;
    public const int MinRegressionPoints = 7;
    public const decimal NoChangeThresholdKg = 0.05m;

    /// <param name="raw">Chuỗi từ <see cref="Daily"/> kết thúc ở <paramref name="today"/>.</param>
    public static WeightTrendInsightsResponse Insights(IReadOnlyList<TrendPoint> raw, DateOnly today, int trendDays, int kcalPerKg)
    {
        if (raw.Count == 0)
            return new WeightTrendInsightsResponse(today, null, 0, null, ChangeWindows.Select(n => new WeightChangeModel(n, null, "Waiting")).ToList(), null, null, null, trendDays, kcalPerKg);

        var current = raw[^1].Trend;
        var byDate = raw.ToDictionary(p => p.Date, p => p.Trend);

        // Thay đổi trend hôm nay so với N ngày trước; chưa có dữ liệu từ N ngày trước thì Waiting
        var changes = ChangeWindows.Select(n =>
        {
            if (!byDate.TryGetValue(raw[^1].Date.AddDays(-n), out var past)) return new WeightChangeModel(n, null, "Waiting");
            var change = Round(current - past);
            return new WeightChangeModel(n, change, Math.Abs(change) < NoChangeThresholdKg ? "NoChange" : change < 0 ? "Decrease" : "Increase");
        }).ToList();

        // Độ dốc hồi quy tuyến tính (bình phương nhỏ nhất) của trend trong 21 ngày gần nhất, kg/tuần; cần ≥ 7 ngày dữ liệu
        var window = raw.Where(p => p.Date >= raw[^1].Date.AddDays(-(RegressionDays - 1))).ToList();
        decimal? weekly = null, deficit = null, projected = null;
        if (window.Count >= MinRegressionPoints)
        {
            var x0 = window[0].Date.DayNumber;
            decimal n = window.Count, sx = 0, sy = 0, sxy = 0, sxx = 0;
            foreach (var p in window)
            {
                var x = (decimal)(p.Date.DayNumber - x0);
                sx += x; sy += p.Trend; sxy += x * p.Trend; sxx += x * x;
            }
            var slopePerDay = (n * sxy - sx * sy) / (n * sxx - sx * sx);
            var perWeek = slopePerDay * 7;
            weekly = Round(perWeek);
            deficit = Math.Round(slopePerDay * kcalPerKg, 0, MidpointRounding.AwayFromZero);   // = tuần × kcal/kg ÷ 7
            projected = Round(current + perWeek * 30 / 7);
        }

        return new WeightTrendInsightsResponse(today, raw[0].Date, raw.Count, Round(current), changes, weekly, deficit, projected, trendDays, kcalPerKg);
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Trang Weight Trend: trend MỖI NGÀY từ lần cân đầu tiên (không trước đó) đến To (mặc định và tối đa hôm nay theo múi giờ user), kèm thống kê trên trend (Average, Difference) và trên số cân thực,
/// và bucket Day/Week/Month khi có Granularity. From mặc định 90 ngày trước To; All = true và không From: từ ngày cân đầu tiên. Trend luôn tính từ lần cân đầu tiên của user (EMA cần cả lịch sử), chỉ phần xuất ra bị cắt theo khoảng.
/// </summary>
public sealed record GetWeightTrendQuery(DateOnly? From, DateOnly? To, bool? All = null, MetricGranularity? Granularity = null) : IRequest<WeightTrendResponse>;

public sealed class GetWeightTrendQueryValidator : AbstractValidator<GetWeightTrendQuery>
{
    public GetWeightTrendQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage("From must not be after To.");
}

public sealed class GetWeightTrendQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetWeightTrendQuery, WeightTrendResponse>
{
    public async Task<WeightTrendResponse> Handle(GetWeightTrendQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To is { } t && t < locale.Today ? t : locale.Today;
        var trendDays = _options.Value.TrendDays;

        // Nguồn duy nhất: các lần cân chưa xoá (Scale Weight); sửa/xoá cân thì lần gọi sau tự tính lại
        var entries = (await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate <= to)
            .Select(w => new { w.LogDate, w.WeightKg })
            .ToListAsync(ct)).Select(w => (w.LogDate, w.WeightKg)).ToList();

        var raw = WeightTrendSeries.Daily(entries, trendDays, to);
        var from = request.From ?? (request.All == true ? (raw.Count > 0 ? raw[0].Date : to) : to.AddDays(-90));
        var days = WeightTrendSeries.ToDays(raw).Where(d => d.Date >= from).ToList();

        var (summary, _) = MetricSeries.Build(days.Select(d => (d.Date, d.TrendKg)), from, to, null, locale.WeekStart);
        var (scaleSummary, _) = MetricSeries.Build(entries, from, to, null, locale.WeekStart);

        var buckets = new List<WeightTrendBucket>();
        if (request.Granularity is { } g)
        {
            var scales = entries.Where(e => e.LogDate >= from && e.LogDate <= to).ToLookup(e => MetricSeries.BucketStart(e.LogDate, g, locale.WeekStart), e => e.WeightKg);
            buckets = days.GroupBy(d => MetricSeries.BucketStart(d.Date, g, locale.WeekStart)).OrderBy(b => b.Key)
                .Select(b => new WeightTrendBucket(b.Key, MetricSeries.BucketEnd(b.Key, g), Math.Round(b.Average(d => d.TrendKg), 2, MidpointRounding.AwayFromZero),
                    scales[b.Key].Any() ? Math.Round(scales[b.Key].Average(), 2, MidpointRounding.AwayFromZero) : null, b.Count(), scales[b.Key].Count()))
                .ToList();
        }

        return new WeightTrendResponse(trendDays, Math.Round(2m / (trendDays + 1), 4), from, to, days, summary, scaleSummary, buckets);
    }
}

/// <summary>
/// Insights &amp; Data: Weight Changes 3/7/14/30/90 ngày (trend hôm nay − trend N ngày trước, Waiting nếu chưa đủ dữ liệu), cân hiện tại (trend hôm nay),
/// thay đổi hằng tuần (hồi quy tuyến tính trend 21 ngày gần nhất), thâm hụt năng lượng (× Algorithm:KcalPerKg ÷ 7) và cân dự kiến sau 30 ngày. Tính đến hôm nay theo múi giờ user.
/// </summary>
public sealed record GetWeightTrendInsightsQuery : IRequest<WeightTrendInsightsResponse>;

public sealed class GetWeightTrendInsightsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetWeightTrendInsightsQuery, WeightTrendInsightsResponse>
{
    public async Task<WeightTrendInsightsResponse> Handle(GetWeightTrendInsightsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var entries = (await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate <= today)
            .Select(w => new { w.LogDate, w.WeightKg })
            .ToListAsync(ct)).Select(w => (w.LogDate, w.WeightKg)).ToList();

        var raw = WeightTrendSeries.Daily(entries, _options.Value.TrendDays, today);
        return WeightTrendSeries.Insights(raw, today, _options.Value.TrendDays, _options.Value.KcalPerKg);
    }
}
