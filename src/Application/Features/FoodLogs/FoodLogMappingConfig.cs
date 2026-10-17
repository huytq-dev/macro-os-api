namespace Application;

public sealed class FoodLogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FoodLogEntry, FoodLogEntryModel>()
            .Map(dest => dest.MealType,    src => src.MealType.ToString())
            .Map(dest => dest.FoodName,    src => src.FoodItem != null ? src.FoodItem.Name : src.Recipe != null ? src.Recipe.Name : "")
            .Map(dest => dest.ServingName, src => src.FoodServing != null ? src.FoodServing.ServingName : null)
            // Emoji từ nhóm món (FoodEmoji gọi ở projection cuối, EF chỉ lấy cột Category); recipe/quick add → null
            .Map(dest => dest.Emoji,       src => FoodEmoji.For(src.FoodItem != null ? (FoodCategory?)src.FoodItem.Category : null))
            // Khối lượng dòng log: số serving × gram của serving; log theo gram thì = quantity; recipe/quick add → null
            .Map(dest => dest.WeightGrams, src => src.FoodItem == null || src.FoodItem.Source == FoodSource.QuickAdd
                ? (decimal?)null
                : src.FoodServing != null ? (decimal?)(src.Quantity * src.FoodServing.WeightInGrams) : src.Quantity);

        config.NewConfig<FoodLogSetting, FoodLogSettingsModel>()
            .Map(dest => dest.Overages,          src => src.Overages.ToString())
            .Map(dest => dest.Alignment,         src => src.Alignment.ToString())
            .Map(dest => dest.AddFoodsToHour,    src => src.AddFoodsToHour.ToString())
            .Map(dest => dest.WeekBanner,        src => src.WeekBanner.ToString())
            .Map(dest => dest.CalendarScrolling, src => src.CalendarScrolling.ToString())
            .Map(dest => dest.Optimization,      src => src.Optimization.ToString())
            .Map(dest => dest.TimelineTileDensity, src => src.TimelineTileDensity.ToString())
            .Map(dest => dest.TimelineHighlight, src => src.TimelineHighlight.ToString())
            .Map(dest => dest.LoggerBannerDisplay, src => src.LoggerBannerDisplay.ToString())
            .Map(dest => dest.LoggerTileDensity, src => src.LoggerTileDensity.ToString())
            .Map(dest => dest.LoggerHighlight, src => src.LoggerHighlight.ToString());
    }
}
