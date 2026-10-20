namespace Application;

public sealed class FoodMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FoodServing, FoodServingModel>()
            .Map(dest => dest.Label, src => FoodServingText.Label(src.ServingName));

        // IsMine cần userId → handler set sau khi Adapt (GetFoodQueryHandler)
        config.NewConfig<FoodItem, FoodItemResponse>()
            .Map(dest => dest.Category,  src => src.Category.ToString())
            .Map(dest => dest.Source,    src => src.Source.ToString())
            .Map(dest => dest.IsMine,    src => false)
            // Chỉ 4 macro; chất khác nằm ở FoodNutrient → FoodNutrientStore.ToResponseAsync điền Nutrition/Nutrients
            .Map(dest => dest.Nutrition, src => new FoodNutrition(src.CaloriesPer100g, src.ProteinPer100g, src.CarbsPer100g, src.FatPer100g, null, null, null, null, null, null, null, null, null, null, null))
            .Map(dest => dest.Nutrients, src => new List<FoodNutrientValue>())
            .Map(dest => dest.Servings, src => src.Servings.OrderByDescending(s => s.IsDefault).ThenBy(s => s.ServingName).ToList());
    }
}
