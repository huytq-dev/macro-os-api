namespace Application;

/// <summary>UC-E04: % mỡ cơ thể (tile Visual Body Fat) mới nhất trước; lọc theo phương pháp đo, mặc định 7 lần gần nhất, tối đa 100.</summary>
public sealed record GetBodyFatQuery(MeasurementMethod? Method, int? Limit) : IRequest<BodyFatResponse>;

public sealed class GetBodyFatQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetBodyFatQuery, BodyFatResponse>
{
    public async Task<BodyFatResponse> Handle(GetBodyFatQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var limit = Math.Clamp(request.Limit ?? 7, 1, 100);

        var query = _uow.Repository<BodyMetric>().Query()
            .Where(b => b.UserId == userId && b.BodyFatPercent != null);
        if (request.Method is { } method) query = query.Where(b => b.Method == method);

        var rows = await query
            .OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt)
            .Take(limit)
            .Select(b => new { b.Id, b.MeasuredDate, b.Method, b.BodyFatPercent, b.Notes })
            .ToListAsync(ct);

        var items = rows.Select(r => new BodyFatEntryModel(r.Id, r.MeasuredDate, r.Method.ToString(), r.BodyFatPercent!.Value, r.Notes)).ToList();
        return new BodyFatResponse(items, items.FirstOrDefault()?.BodyFatPercent);
    }
}
