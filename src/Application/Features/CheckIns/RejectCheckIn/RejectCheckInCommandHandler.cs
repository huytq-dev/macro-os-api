namespace Application;

public sealed class RejectCheckInCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<RejectCheckInCommand, Guid>
{
    public async Task<Guid> Handle(RejectCheckInCommand request, CancellationToken ct)
        => await CheckInSupport.DecideAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), _options.Value, accepted: false, ct);
}
