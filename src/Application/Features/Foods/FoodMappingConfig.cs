namespace Application;

public sealed class FoodMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FoodServing, FoodServingModel>();

        // IsMine cần userId → handler set sau khi Adapt (GetFoodQueryHandler)
        config.NewConfig<FoodItem, FoodItemResponse>()
            .Map(dest => dest.Category,  src => src.Category.ToString())
            .Map(dest => dest.Source,    src => src.Source.ToString())
            .Map(dest => dest.IsMine,    src => false)
            .Map(dest => dest.Nutrition, src => new FoodNutrition(
                src.CaloriesPer100g, src.ProteinPer100g, src.CarbsPer100g, src.FatPer100g,
                src.FiberPer100g, src.SugarPer100g, src.SodiumPer100g, src.PotassiumPer100g,
                src.CalciumPer100g, src.IronPer100g, src.VitaminCPer100g, src.VitaminDPer100g,
                src.SaturatedFatPer100g, src.TransFatPer100g, src.CholesterolPer100g))
            .Map(dest => dest.Servings, src => src.Servings.OrderByDescending(s => s.IsDefault).ThenBy(s => s.ServingName).ToList());
    }
}
