namespace Application;

public sealed class AcceptCheckInCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<AcceptCheckInCommand, Guid>
{
    public async Task<Guid> Handle(AcceptCheckInCommand request, CancellationToken ct)
        => await CheckInSupport.DecideAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), _options.Value, accepted: true, ct);
}
