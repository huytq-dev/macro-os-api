namespace Application;

/// <summary>Cài đặt trang Food Log của user; chưa lưu lần nào thì trả mặc định (không ghi DB).</summary>
public sealed record GetFoodLogSettingsQuery : IRequest<FoodLogSettingsModel>;

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

/// <summary>Thay toàn bộ cài đặt (idempotent, tạo dòng nếu chưa có). Thiếu trường trong body = giá trị đầu của enum / false / 0, nên FE gửi đủ object.</summary>
public sealed record UpdateFoodLogSettingsCommand(
    OverageDisplay Overages,
    int HourRangeFirst,
    int HourRangeLast,
    TimelineAlignment Alignment,
    AddFoodTrigger AddFoodsToHour,
    bool ShowTimestamps,
    bool ShowHourlyTotals,
    WeekBannerMode WeekBanner,
    CalendarScrollMode CalendarScrolling,
    bool PremoveEnabled,
    bool ShowBrandedResults,
    bool ShowOpenFoodFactsResults,
    SearchOptimization Optimization,
    TileDensity TimelineTileDensity,
    bool TimelineShowMacros,
    bool TimelineShowWeight,
    bool TimelineShowServing,
    TileHighlight TimelineHighlight,
    bool LoggerBannerShowCalories,
    bool LoggerBannerShowProtein,
    bool LoggerBannerShowFat,
    bool LoggerBannerShowCarbs,
    BannerDisplay LoggerBannerDisplay,
    TileDensity LoggerTileDensity,
    bool LoggerShowMacros,
    bool LoggerShowWeight,
    bool LoggerShowServing,
    SearchRowHighlight LoggerHighlight
) : IRequest<Unit>;

public sealed class UpdateFoodLogSettingsCommandValidator : AbstractValidator<UpdateFoodLogSettingsCommand>
{
    public UpdateFoodLogSettingsCommandValidator()
    {
        RuleFor(x => x.Overages).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Alignment).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.AddFoodsToHour).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.WeekBanner).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.CalendarScrolling).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Optimization).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.TimelineTileDensity).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.TimelineHighlight).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.LoggerBannerDisplay).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.LoggerTileDensity).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.LoggerHighlight).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);

        RuleFor(x => x.HourRangeFirst).InclusiveBetween(0, 23).WithMessage(ValidationMessages.Range);
        RuleFor(x => x.HourRangeLast).InclusiveBetween(0, 23).WithMessage(ValidationMessages.Range);
        RuleFor(x => x).Must(x => x.HourRangeLast - x.HourRangeFirst >= 1)
            .WithMessage("HourRangeLast phải lớn hơn HourRangeFirst ít nhất 1 giờ.")
            .When(x => x.HourRangeFirst is >= 0 and <= 23 && x.HourRangeLast is >= 0 and <= 23);
    }
}

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
