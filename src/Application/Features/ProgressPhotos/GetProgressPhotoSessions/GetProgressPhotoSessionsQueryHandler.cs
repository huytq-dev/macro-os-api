namespace Application;

public sealed class GetProgressPhotoSessionsQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<ProgressPhotoOptions> _options, IOptions<JwtOptions> _jwt)
    : IRequestHandler<GetProgressPhotoSessionsQuery, ProgressPhotoSessionsResponse>
{
    public async Task<ProgressPhotoSessionsResponse> Handle(GetProgressPhotoSessionsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var now = _timeProvider.GetUtcNow();
        var consent = await ProgressPhotoSupport.FirstConsentAsync(_uow, userId, ct);

        var query = _uow.Repository<ProgressPhoto>().Query().Where(p => p.UserId == userId);
        if (request.From is { } from) query = query.Where(p => p.PhotoDate >= from);
        if (request.To is { } to) query = query.Where(p => p.PhotoDate <= to);

        if (request.Limit is { } limit)
        {
            var dates = await query.Select(p => p.PhotoDate).Distinct().OrderByDescending(d => d).Take(Math.Clamp(limit, 1, 1000)).ToListAsync(ct);
            if (dates.Count == 0) return new ProgressPhotoSessionsResponse(consent is not null, []);
            query = query.Where(p => dates.Contains(p.PhotoDate));
        }
        var rows = await query.ToListAsync(ct);
        if (rows.Count == 0) return new ProgressPhotoSessionsResponse(consent is not null, []);

        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, rows.Min(r => r.PhotoDate), rows.Max(r => r.PhotoDate), ct);
        var sessions = rows.GroupBy(r => r.PhotoDate).OrderByDescending(g => g.Key).Select(g =>
        {
            var has = weights.TryGetValue(g.Key, out var w);
            return new ProgressPhotoSessionModel(g.Key,
                g.OrderBy(r => (int)r.Angle).Select(r => ProgressPhotoSupport.ToModel(r, _jwt.Value.SecretKey, _options.Value, now)).ToList(),
                has ? w.Scale : null, has ? w.Trend : null);
        }).ToList();
        return new ProgressPhotoSessionsResponse(consent is not null, sessions);
    }
}
