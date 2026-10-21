namespace Application;

public sealed class UpdatePeriodLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdatePeriodLogCommand, Unit>
{
    public async Task<Unit> Handle(UpdatePeriodLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var logs = (await _uow.Repository<PeriodLog>().GetAllTrackedAsync(p => p.UserId == userId, ct)).ToList();
        var log = logs.FirstOrDefault(p => p.Id == request.Id)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("PeriodLog", request.Id));
        if (logs.Any(p => p.Id != log.Id && p.StartDate == request.StartDate))
            throw new ConflictException(ExceptionMessages.AlreadyExists("PeriodLog", request.StartDate));

        log.StartDate = request.StartDate;
        log.EndDate = request.EndDate;
        log.Symptoms = request.Symptoms;
        log.Notes = request.Notes;
        PeriodSupport.Recompute(logs);

        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
