namespace Application;

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
