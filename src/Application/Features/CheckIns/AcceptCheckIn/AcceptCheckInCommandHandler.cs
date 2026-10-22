namespace Application;

public sealed class AcceptCheckInCommandHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<AcceptCheckInCommand, Guid>
{
    public async Task<Guid> Handle(AcceptCheckInCommand request, CancellationToken ct)
        => await CheckInSupport.DecideAsync(_uow, _userContext.UserId, (await _clock.GetAsync(ct)).Today, _options.Value, accepted: true, ct);
}
