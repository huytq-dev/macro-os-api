namespace Application;

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
        var direction = goal is { } g ? GoalRules.DirectionOf(g) switch { GoalDirection.Loss => "Decrease", GoalDirection.Gain => "Increase", _ => "Neutral" } : "Neutral";

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
