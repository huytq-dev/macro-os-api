namespace Application;

public sealed class ExpenditureMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ExpenditureSnapshot, ExpenditurePointModel>()
            .Map(dest => dest.Status, src => src.Status.ToString());
        config.NewConfig<DailyProgressSnapshot, DailyProgressModel>();
    }
}
