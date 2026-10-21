namespace Application;

public sealed class SetIncompleteDaysCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<SetIncompleteDaysCommand, CheckInSessionResponse>
{
    public async Task<CheckInSessionResponse> Handle(SetIncompleteDaysCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = FoodLogSupport.Today(_timeProvider);
        var from = today.AddDays(-6);

        var wanted = request.Dates.ToHashSet();
        if (wanted.Any(d => d < from || d > today))
            throw new BadRequestException(ExceptionMessages.IncompleteDaysOutOfRange);

        var logs = await _uow.Repository<FoodLog>().Query(tracking: true)
            .Include(l => l.Entries)
            .Where(l => l.UserId == userId && l.LogDate >= from && l.LogDate <= today)
            .ToListAsync(ct);

        DateOnly? earliest = null;
        foreach (var log in logs)
        {
            var flag = wanted.Contains(log.LogDate) && log.Entries.Count > 0; // ngày chưa log (không có entry) không đánh dấu được
            if (log.IsIncomplete == flag) continue;
            log.IsIncomplete = flag;
            if (earliest is null || log.LogDate < earliest) earliest = log.LogDate;
        }

        if (earliest is { } changedFrom)
        {
            await _uow.EnsureSaveAsync(ct);
            await ExpenditureRebuilder.RebuildAsync(_uow, userId, changedFrom, today, _options.Value, ct);
            await _uow.SaveChangesAsync(ct);
        }

        return await CheckInSupport.BuildSessionAsync(_uow, userId, today, _options.Value, ct);
    }
}
