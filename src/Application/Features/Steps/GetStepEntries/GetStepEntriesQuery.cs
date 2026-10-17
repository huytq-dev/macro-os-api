namespace Application;

/// <summary>UC-E07: số bước theo ngày (mới nhất trước); mặc định 30 ngày gần nhất.</summary>
public sealed record GetStepEntriesQuery(DateOnly? From, DateOnly? To) : IRequest<StepEntriesResponse>;

public sealed class GetStepEntriesQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetStepEntriesQuery, StepEntriesResponse>
{
    public async Task<StepEntriesResponse> Handle(GetStepEntriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var to = request.To ?? FoodLogSupport.Today(_timeProvider);
        var from = request.From ?? to.AddDays(-29);

        var items = await _uow.Repository<StepEntry>().Query()
            .Where(s => s.UserId == userId && s.LogDate >= from && s.LogDate <= to)
            .OrderByDescending(s => s.LogDate)
            .Select(s => new StepEntryModel(s.Id, s.LogDate, s.Steps, s.Source.ToString()))
            .ToListAsync(ct);

        var recent = items.Where(i => i.LogDate > to.AddDays(-7)).ToList();
        int? average = recent.Count == 0 ? null : (int)Math.Round(recent.Average(i => i.Steps));
        return new StepEntriesResponse(items, average);
    }
}
