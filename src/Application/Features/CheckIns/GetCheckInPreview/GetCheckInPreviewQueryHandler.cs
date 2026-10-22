namespace Application;

public sealed class GetCheckInPreviewQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetCheckInPreviewQuery, CheckInPreviewResponse>
{
    public async Task<CheckInPreviewResponse> Handle(GetCheckInPreviewQuery request, CancellationToken ct)
        => (await CheckInSupport.BuildAsync(_uow, _userContext.UserId, (await _clock.GetAsync(ct)).Today, _options.Value, ct)).Preview;
}
