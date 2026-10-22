namespace Application;

public sealed class GetExpenditureSettingsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetExpenditureSettingsQuery, ExpenditureSettingsResponse>
{
    public async Task<ExpenditureSettingsResponse> Handle(GetExpenditureSettingsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        return await ExpenditureSettingsSupport.BuildResponseAsync(_uow, userId, today, ct);
    }
}
