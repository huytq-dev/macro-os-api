namespace Application;

public sealed class FoodLogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FoodLogEntry, FoodLogEntryModel>()
            .Map(dest => dest.MealType,    src => src.MealType.ToString())
            .Map(dest => dest.FoodName,    src => src.FoodItem != null ? src.FoodItem.Name : src.Recipe != null ? src.Recipe.Name : "")
            .Map(dest => dest.ServingName, src => src.FoodServing != null ? src.FoodServing.ServingName : null);
    }
}
