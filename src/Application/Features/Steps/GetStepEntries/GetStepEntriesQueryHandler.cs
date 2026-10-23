namespace Application;

public sealed class GetStepEntriesQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetStepEntriesQuery, StepEntriesResponse>
{
    public async Task<StepEntriesResponse> Handle(GetStepEntriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var to = request.To ?? (await _clock.GetAsync(ct)).Today;
        var from = request.From ?? to.AddDays(-29);

        var items = await _uow.Repository<StepEntry>().Query()
            .Where(s => s.UserId == userId && s.LogDate >= from && s.LogDate <= to)
            .OrderByDescending(s => s.LogDate)
            .Select(s => new StepEntryModel(s.Id, s.LogDate, s.Steps, s.Source.ToString()))
            .ToListAsync(ct);

        return new StepEntriesResponse(items, StepSummary.Last7DayAverage(items, to));
    }
}
