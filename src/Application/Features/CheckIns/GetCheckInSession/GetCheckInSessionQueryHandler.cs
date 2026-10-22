namespace Application;

public sealed class GetCheckInSessionQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetCheckInSessionQuery, CheckInSessionResponse>
{
    public async Task<CheckInSessionResponse> Handle(GetCheckInSessionQuery request, CancellationToken ct)
        => await CheckInSupport.BuildSessionAsync(_uow, _userContext.UserId, (await _clock.GetAsync(ct)).Today, _options.Value, ct);
}
