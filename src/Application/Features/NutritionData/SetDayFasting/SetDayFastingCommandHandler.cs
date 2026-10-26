namespace Application;

public sealed class SetDayFastingCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<SetDayFastingCommand, NutritionDataDayModel>
{
    public async Task<NutritionDataDayModel> Handle(SetDayFastingCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        locale.EnsureNotFuture(request.Date, "Date");

        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.Date, ct);
        var live = NutritionDataSupport.Live(log);

        if (request.IsFasting)
        {
            if (live.Count > 0) throw NutritionDataSupport.FastingBlocked();   // không xoá, không đổi
            if (NutritionDataSupport.ManualPositive(log.ManualCaloriesKcal, log.ManualProteinG, log.ManualCarbsG, log.ManualFatG)) throw NutritionDataSupport.FastingWithManual();
        }

        var changed = log.IsFasting != request.IsFasting;
        log.IsFasting = request.IsFasting;
        changed |= NutritionDataSupport.ClearStaleIncomplete(log, live);   // bỏ nhịn ăn trên ngày trống thật ⇒ cờ chưa-đủ cũ cũng bỏ (R2-4)
        if (changed) await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.Date, _options.Value, _timeProvider, ct);
        return NutritionDataSupport.ToModel(log, live);
    }
}
