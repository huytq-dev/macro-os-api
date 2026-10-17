namespace Application;

public sealed class ExpenditureMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ExpenditureSnapshot, ExpenditurePointModel>()
            .Map(dest => dest.Status, src => src.Status.ToString());
        // EstimatedTdeeKcal không có nguồn trên snapshot (Mapster để mặc định null); GetDailyProgressQueryHandler gán từ ExpenditureSnapshot
        config.NewConfig<DailyProgressSnapshot, DailyProgressModel>();
    }
}
