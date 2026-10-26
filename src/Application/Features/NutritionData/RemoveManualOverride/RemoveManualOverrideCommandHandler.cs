namespace Application;

public sealed class RemoveManualOverrideCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<RemoveManualOverrideCommand, NutritionDataDayModel>
{
    public async Task<NutritionDataDayModel> Handle(RemoveManualOverrideCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await _uow.Repository<FoodLog>().Query(tracking: true).Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == request.Date, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLog", request.Date));

        // Chỉ xoá 4 cột manual: không đụng entry, ghi chú, nước, nhịn ăn; hiệu lực quay về Food Log nếu có. Ngày thành trống thật ⇒ tự bỏ cờ chưa-đủ (R2-4), cùng SaveChanges
        var live = NutritionDataSupport.Live(log);
        var changed = NutritionDataSupport.ClearManual(log);
        changed |= NutritionDataSupport.ClearStaleIncomplete(log, live);
        if (changed) await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.Date, _options.Value, _timeProvider, ct);
        return NutritionDataSupport.ToModel(log, live);
    }
}
