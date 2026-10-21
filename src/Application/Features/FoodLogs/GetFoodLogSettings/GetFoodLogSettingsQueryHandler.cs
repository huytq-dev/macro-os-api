namespace Application;

public sealed class GetFoodLogSettingsQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetFoodLogSettingsQuery, FoodLogSettingsModel>
{
    public async Task<FoodLogSettingsModel> Handle(GetFoodLogSettingsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var settings = await _uow.Repository<FoodLogSetting>().FindAsync(s => s.UserId == userId, ct) ?? new FoodLogSetting();
        return settings.Adapt<FoodLogSettingsModel>();
    }
}
