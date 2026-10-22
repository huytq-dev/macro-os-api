namespace Application;

public sealed class RejectCheckInCommandHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<RejectCheckInCommand, Guid>
{
    public async Task<Guid> Handle(RejectCheckInCommand request, CancellationToken ct)
        => await CheckInSupport.DecideAsync(_uow, _userContext.UserId, (await _clock.GetAsync(ct)).Today, _options.Value, accepted: false, ct);
}
