namespace Application;

public sealed class UpdateFoodLogSettingsCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateFoodLogSettingsCommand, Unit>
{
    public async Task<Unit> Handle(UpdateFoodLogSettingsCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var settings = await _uow.Repository<FoodLogSetting>().FindTrackedAsync(s => s.UserId == userId, ct);
        if (settings is null)
        {
            settings = new FoodLogSetting { Id = Guid.NewGuid(), UserId = userId };
            await _uow.Repository<FoodLogSetting>().AddAsync(settings, ct);
        }

        settings.Overages = request.Overages;
        settings.HourRangeFirst = request.HourRangeFirst;
        settings.HourRangeLast = request.HourRangeLast;
        settings.Alignment = request.Alignment;
        settings.AddFoodsToHour = request.AddFoodsToHour;
        settings.ShowTimestamps = request.ShowTimestamps;
        settings.ShowHourlyTotals = request.ShowHourlyTotals;
        settings.WeekBanner = request.WeekBanner;
        settings.CalendarScrolling = request.CalendarScrolling;
        settings.PremoveEnabled = request.PremoveEnabled;
        settings.ShowBrandedResults = request.ShowBrandedResults;
        settings.ShowOpenFoodFactsResults = request.ShowOpenFoodFactsResults;
        settings.Optimization = request.Optimization;
        settings.TimelineTileDensity = request.TimelineTileDensity;
        settings.TimelineShowMacros = request.TimelineShowMacros;
        settings.TimelineShowWeight = request.TimelineShowWeight;
        settings.TimelineShowServing = request.TimelineShowServing;
        settings.TimelineHighlight = request.TimelineHighlight;
        settings.LoggerBannerShowCalories = request.LoggerBannerShowCalories;
        settings.LoggerBannerShowProtein = request.LoggerBannerShowProtein;
        settings.LoggerBannerShowFat = request.LoggerBannerShowFat;
        settings.LoggerBannerShowCarbs = request.LoggerBannerShowCarbs;
        settings.LoggerBannerDisplay = request.LoggerBannerDisplay;
        settings.LoggerTileDensity = request.LoggerTileDensity;
        settings.LoggerShowMacros = request.LoggerShowMacros;
        settings.LoggerShowWeight = request.LoggerShowWeight;
        settings.LoggerShowServing = request.LoggerShowServing;
        settings.LoggerHighlight = request.LoggerHighlight;

        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
