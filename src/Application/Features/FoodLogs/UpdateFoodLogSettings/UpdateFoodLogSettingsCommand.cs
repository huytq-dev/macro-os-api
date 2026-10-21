namespace Application;

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
