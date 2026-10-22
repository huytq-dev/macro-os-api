namespace Application;

public sealed class GetDayWaterQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetDayWaterQuery, DayWaterResponse>
{
    public async Task<DayWaterResponse> Handle(GetDayWaterQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? (await _clock.GetAsync(ct)).Today;
        var rows = await _uow.Repository<WaterLog>().Query()
            .Where(w => w.UserId == userId && w.FoodLog.LogDate == date)
            .OrderBy(w => w.LoggedAt).ThenBy(w => w.Id)
            .Select(w => new { w.Id, w.AmountMl, w.LoggedAt, w.Source })
            .ToListAsync(ct);

        var logs = rows.Select(w => new WaterLogModel(w.Id, w.AmountMl, TimeOnly.FromTimeSpan(w.LoggedAt.TimeOfDay), w.Source)).ToList();
        return new DayWaterResponse(date, logs.Sum(l => l.AmountMl), logs);
    }
}
