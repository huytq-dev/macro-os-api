namespace Application;

public sealed record DeletePeriodLogCommand(Guid Id) : IRequest<Unit>;

public sealed class DeletePeriodLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<DeletePeriodLogCommand, Unit>
{
    public async Task<Unit> Handle(DeletePeriodLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var logs = (await _uow.Repository<PeriodLog>().GetAllTrackedAsync(p => p.UserId == userId, ct)).ToList();
        var log = logs.FirstOrDefault(p => p.Id == request.Id)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("PeriodLog", request.Id));

        await _uow.Repository<PeriodLog>().RemoveAsync(log, ct);
        PeriodSupport.Recompute(logs.Where(p => p.Id != log.Id));

        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
