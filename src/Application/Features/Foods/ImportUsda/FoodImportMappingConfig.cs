namespace Application;

public sealed class FoodImportMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
        => config.NewConfig<FoodImportRun, FoodImportRunModel>().Map(d => d.Status, s => s.Status.ToString());
}
