namespace Application;

public sealed class GetCheckInSessionQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetCheckInSessionQuery, CheckInSessionResponse>
{
    public async Task<CheckInSessionResponse> Handle(GetCheckInSessionQuery request, CancellationToken ct)
        => await CheckInSupport.BuildSessionAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), _options.Value, ct);
}
