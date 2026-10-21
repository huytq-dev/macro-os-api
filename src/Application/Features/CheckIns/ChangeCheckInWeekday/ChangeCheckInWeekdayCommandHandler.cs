namespace Application;

public sealed class ChangeCheckInWeekdayCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<ChangeCheckInWeekdayCommand, Unit>
{
    public async Task<Unit> Handle(ChangeCheckInWeekdayCommand request, CancellationToken ct)
    {
        var program = await _uow.Repository<MacroProgram>().FindTrackedAsync(p => p.UserId == _userContext.UserId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);
        program.CheckInWeekday = request.CheckInWeekday;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
