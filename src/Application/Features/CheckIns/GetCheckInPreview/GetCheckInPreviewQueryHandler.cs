namespace Application;

public sealed class GetCheckInPreviewQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetCheckInPreviewQuery, CheckInPreviewResponse>
{
    public async Task<CheckInPreviewResponse> Handle(GetCheckInPreviewQuery request, CancellationToken ct)
        => (await CheckInSupport.BuildAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), _options.Value, ct)).Preview;
}
