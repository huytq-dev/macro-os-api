namespace Application;

/// <summary>
/// UC-E04: % mỡ cơ thể mới nhất trước; lọc theo phương pháp đo. Không có From/To: mặc định 7 lần gần nhất, tối đa 100 (Limit).
/// Có From và/hoặc To: trả MỌI dòng có % mỡ trong [From, To] (gồm cả hai đầu, bỏ qua Limit), MeasuredDate rồi CreatedAt giảm dần; LatestPercent vẫn là lần mới nhất của phương pháp đó (cả ngoài khoảng).
/// </summary>
public sealed record GetBodyFatQuery(MeasurementMethod? Method, int? Limit, DateOnly? From = null, DateOnly? To = null) : IRequest<BodyFatResponse>;

public sealed class GetBodyFatQueryValidator : AbstractValidator<GetBodyFatQuery>
{
    public GetBodyFatQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithName("From").WithMessage("From must not be after To.");
}

public sealed class GetBodyFatQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetBodyFatQuery, BodyFatResponse>
{
    public async Task<BodyFatResponse> Handle(GetBodyFatQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var ranged = request.From is not null || request.To is not null;

        var all = _uow.Repository<BodyMetric>().Query()
            .Where(b => b.UserId == userId && b.BodyFatPercent != null);
        if (request.Method is { } method) all = all.Where(b => b.Method == method);

        var query = all;
        if (request.From is { } from) query = query.Where(b => b.MeasuredDate >= from);
        if (request.To is { } to) query = query.Where(b => b.MeasuredDate <= to);
        query = query.OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt);
        if (!ranged) query = query.Take(Math.Clamp(request.Limit ?? 7, 1, 100));

        var rows = await query.Select(b => new { b.Id, b.MeasuredDate, b.Method, b.BodyFatPercent, b.Notes }).ToListAsync(ct);
        var items = rows.Select(r => new BodyFatEntryModel(r.Id, r.MeasuredDate, r.Method.ToString(), r.BodyFatPercent!.Value, r.Notes)).ToList();

        // Có khoảng: lần mới nhất của phương pháp đó có thể nằm ngoài khoảng → hỏi riêng (chỉ khi khoảng không chứa nó)
        decimal? latest = ranged
            ? await all.OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt).Select(b => b.BodyFatPercent).FirstOrDefaultAsync(ct)
            : items.FirstOrDefault()?.BodyFatPercent;
        return new BodyFatResponse(items, latest);
    }
}
