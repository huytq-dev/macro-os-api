namespace Application;

public sealed class CreatePeriodLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<CreatePeriodLogCommand, Guid>
{
    public async Task<Guid> Handle(CreatePeriodLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var logs = (await _uow.Repository<PeriodLog>().GetAllTrackedAsync(p => p.UserId == userId, ct)).ToList();
        if (logs.Any(p => p.StartDate == request.StartDate))
            throw new ConflictException(ExceptionMessages.AlreadyExists("PeriodLog", request.StartDate));

        var log = new PeriodLog
        {
            Id = Guid.NewGuid(), UserId = userId, StartDate = request.StartDate, EndDate = request.EndDate,
            Symptoms = request.Symptoms, Notes = request.Notes,
        };
        await _uow.Repository<PeriodLog>().AddAsync(log, ct);
        logs.Add(log);
        PeriodSupport.Recompute(logs);

        await _uow.SaveChangesAsync(ct);
        return log.Id;
    }
}
