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

/// <summary>
/// Lưu MỘT PHẦN của một ngày trong một lần (một transaction): chỉ các ô được gửi (không null) bị ghi/ghi đè, các ô còn lại GIỮ NGUYÊN;
/// <paramref name="Clear"/> liệt kê khoá chỉ số cần xoá (số đo vòng như "waist", "leftBicep" hoặc "visualBodyFat"). Khác PUT full-body (thay cả ngày).
/// Date null = hôm nay theo múi giờ user, không ở tương lai. Một chỉ số một giá trị mỗi ngày (ghi lại là ghi đè).
/// </summary>
public sealed record PatchFullBodyCommand(DateOnly? Date, decimal? VisualBodyFatPercent, TapeMeasurementsModel? Tape, List<string>? Clear) : IRequest<FullBodyResponse>
{
    /// <summary>khoá → giá trị cần ghi, hoặc null = xoá.</summary>
    public Dictionary<string, decimal?> Changes()
    {
        var changes = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        if (VisualBodyFatPercent is { } percent) changes[BodyMeasurements.VisualBodyFatKey] = percent;
        if (Tape is { } tape)
            foreach (var m in BodyMeasurements.All)
                if (m.FromModel(tape) is { } cm) changes[m.Key] = cm;
        foreach (var key in Clear ?? []) changes.TryAdd(BodyMeasurements.Find(key)?.Key ?? key, null);
        return changes;
    }
}

public sealed class PatchFullBodyCommandValidator : AbstractValidator<PatchFullBodyCommand>
{
    public PatchFullBodyCommandValidator()
    {
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.Date.HasValue);
        RuleFor(x => x.VisualBodyFatPercent)
            .InclusiveBetween(BodyMeasurements.VisualBodyFatMin, BodyMeasurements.VisualBodyFatMax).WithMessage(ValidationMessages.Range)
            .When(x => x.VisualBodyFatPercent.HasValue);
        foreach (var m in BodyMeasurements.All)
            RuleFor(x => x.Tape).Must(t => m.FromModel(t!) is null || (m.FromModel(t!) >= m.Min && m.FromModel(t!) <= m.Max))
                .WithMessage(ValidationMessages.TapeMeasurementRange(m.ModelName, m.Min, m.Max))
                .When(x => x.Tape is not null);
        RuleFor(x => x.Clear).Must(c => c is null || c.All(BodyMeasurements.IsKnownKey))
            .WithMessage(ValidationMessages.UnknownMeasurementKey);
        RuleFor(x => x).Must(x => !(x.Clear ?? []).Any(k => x.Changes().TryGetValue(BodyMeasurements.Find(k)?.Key ?? k, out var v) && v is not null))
            .WithName("Clear").WithMessage(ValidationMessages.MeasurementSetAndClear);
        RuleFor(x => x).Must(x => x.Changes().Count > 0).WithName("Tape").WithMessage(ValidationMessages.NothingToChange);
    }
}

public sealed class PatchFullBodyCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<PatchFullBodyCommand, FullBodyResponse>
{
    public async Task<FullBodyResponse> Handle(PatchFullBodyCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var date = request.Date ?? locale.Today;
        locale.EnsureNotFuture(date, "Date");

        var (visual, tape, _) = await FullBodyWriter.ApplyAsync(_uow, userId, date, request.Changes(), ct);
        await _uow.EnsureSaveAsync(ct);

        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, date, date, ct);
        return FullBodySupport.Build(date, visual, tape, weights.TryGetValue(date, out var w) ? w.Scale : null, weights.TryGetValue(date, out w) ? w.Trend : null);
    }
}

/// <summary>
/// Danh sách các ngày có dữ liệu Full Body (mới nhất trước) kèm số chỉ số, % mỡ Visual, số đo vòng, cân và trend của ngày đó. From/To tuỳ chọn (gồm hai đầu), mặc định mọi ngày.
/// Chỉ trả ngày có ít nhất một chỉ số; hàng đã xoá mềm bị loại.
/// </summary>
public sealed record GetFullBodyHistoryQuery(DateOnly? From, DateOnly? To) : IRequest<FullBodyHistoryResponse>;

public sealed class GetFullBodyHistoryQueryValidator : AbstractValidator<GetFullBodyHistoryQuery>
{
    public GetFullBodyHistoryQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage("From must not be after To.");
}

public sealed class GetFullBodyHistoryQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetFullBodyHistoryQuery, FullBodyHistoryResponse>
{
    public async Task<FullBodyHistoryResponse> Handle(GetFullBodyHistoryQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var query = _uow.Repository<BodyMetric>().Query()
            .Where(b => b.UserId == userId && (b.Method == MeasurementMethod.Visual || b.Method == MeasurementMethod.Tape));
        if (request.From is { } from) query = query.Where(b => b.MeasuredDate >= from);
        if (request.To is { } to) query = query.Where(b => b.MeasuredDate <= to);
        var rows = await query.OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt).ToListAsync(ct);

        var days = rows.GroupBy(r => r.MeasuredDate).Select(g =>
        {
            var visual = g.FirstOrDefault(r => r.Method == MeasurementMethod.Visual && r.BodyFatPercent != null);
            var tape = g.FirstOrDefault(r => r.Method == MeasurementMethod.Tape && BodyMeasurements.HasAny(r));
            return (Date: g.Key, Visual: visual, Tape: tape, Count: (visual is null ? 0 : 1) + BodyMeasurements.CountValues(tape));
        }).Where(d => d.Count > 0).ToList();
        if (days.Count == 0) return new FullBodyHistoryResponse([]);

        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, days.Min(d => d.Date), days.Max(d => d.Date), ct);
        var items = days.Select(d =>
        {
            var has = weights.TryGetValue(d.Date, out var w);
            return new FullBodyHistoryItem(d.Date, d.Count, d.Visual?.BodyFatPercent, d.Tape is null ? null : FullBodySupport.ToModel(d.Tape),
                has ? w.Scale : null, has ? w.Trend : null);
        }).ToList();
        return new FullBodyHistoryResponse(items);
    }
}

/// <summary>
/// So sánh hai ngày: mỗi chỉ số có ở ít nhất một ngày → giá trị A, giá trị B, thay đổi (B − A, null nếu thiếu một bên), xu hướng (Up | Down | Same) và đánh giá theo mục tiêu của user
/// (program đang chạy: WeightLoss → giảm là Good; WeightGain/MuscleGain → tăng là Good; còn lại Neutral). % mỡ Visual: giảm luôn là Good. Nhóm WeightAndBodyFat gồm cân, trend và % mỡ Visual.
/// </summary>
public sealed record GetFullBodyCompareQuery(DateOnly? DateA, DateOnly? DateB) : IRequest<FullBodyCompareResponse>;

public sealed class GetFullBodyCompareQueryValidator : AbstractValidator<GetFullBodyCompareQuery>
{
    public GetFullBodyCompareQueryValidator()
    {
        RuleFor(x => x.DateA).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.DateB).NotNull().WithMessage(ValidationMessages.Required);
    }
}

public sealed class GetFullBodyCompareQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetFullBodyCompareQuery, FullBodyCompareResponse>
{
    public const string WeightAndBodyFat = "WeightAndBodyFat";

    public async Task<FullBodyCompareResponse> Handle(GetFullBodyCompareQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var a = request.DateA!.Value;
        var b = request.DateB!.Value;

        var goal = await _uow.Repository<MacroProgram>().Query()
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => (GoalType?)p.GoalType).FirstOrDefaultAsync(ct);
        var direction = goal switch { GoalType.WeightLoss => "Decrease", GoalType.WeightGain or GoalType.MuscleGain => "Increase", _ => "Neutral" };

        var (visualA, tapeA) = await FullBodySupport.LoadAsync(_uow, userId, a, tracking: false, ct);
        var (visualB, tapeB) = await FullBodySupport.LoadAsync(_uow, userId, b, tracking: false, ct);
        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, a < b ? a : b, a < b ? b : a, ct);
        decimal? Scale(DateOnly d) => weights.TryGetValue(d, out var w) ? w.Scale : null;
        decimal? Trend(DateOnly d) => weights.TryGetValue(d, out var w) ? w.Trend : null;

        var items = new List<FullBodyCompareItem>();
        void Add(string key, string group, decimal? va, decimal? vb, bool decreaseIsAlwaysGood = false)
        {
            if (va is null && vb is null) return;
            decimal? delta = va is { } x && vb is { } y ? Math.Round(y - x, 2, MidpointRounding.AwayFromZero) : null;
            var trend = delta is null ? null : delta > 0 ? "Up" : delta < 0 ? "Down" : "Same";
            var dir = decreaseIsAlwaysGood ? "Decrease" : direction;
            var quality = delta is null ? null
                : delta == 0 || dir == "Neutral" ? "Neutral"
                : (delta < 0) == (dir == "Decrease") ? "Good" : "Bad";
            items.Add(new FullBodyCompareItem(key, group, va, vb, delta, trend, quality));
        }

        Add("scaleWeight", WeightAndBodyFat, Scale(a), Scale(b));
        Add("trendWeight", WeightAndBodyFat, Trend(a), Trend(b));
        Add(BodyMeasurements.VisualBodyFatKey, WeightAndBodyFat, visualA?.BodyFatPercent, visualB?.BodyFatPercent, decreaseIsAlwaysGood: true);
        foreach (var m in BodyMeasurements.All)
            Add(m.Key, m.Group, tapeA is null ? null : m.Get(tapeA), tapeB is null ? null : m.Get(tapeB));

        return new FullBodyCompareResponse(a, b, direction, items);
    }
}
