namespace Application;

public sealed class DeleteStepEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<DeleteStepEntryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteStepEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var entry = await _uow.Repository<StepEntry>().FindTrackedAsync(s => s.UserId == userId && s.LogDate == request.LogDate, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFoundField("StepEntry", "LogDate", request.LogDate));

        await _uow.Repository<StepEntry>().RemoveAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
