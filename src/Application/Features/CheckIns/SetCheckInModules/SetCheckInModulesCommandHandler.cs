namespace Application;

public sealed class SetCheckInModulesCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<SetCheckInModulesCommand, Unit>
{
    public async Task<Unit> Handle(SetCheckInModulesCommand request, CancellationToken ct)
    {
        var profile = await _uow.Repository<UserProfile>().FindTrackedAsync(p => p.UserId == _userContext.UserId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", _userContext.UserId));

        if (request.IntroSeen is { } intro) profile.CheckInIntroSeen = intro;
        if (request.PartialLoggingHidden is { } hidden) profile.PartialLoggingModuleHidden = hidden;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
