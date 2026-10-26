namespace Application;

public sealed class DeleteFoodLogEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<DeleteFoodLogEntryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFoodLogEntryCommand request, CancellationToken ct)
    {
        var entry = await FoodLogSupport.GetOwnedEntryAsync(_uow, _userContext.UserId, request.Id, ct);
        await _uow.Repository<FoodLogEntry>().RemoveAsync(entry, ct); // soft delete qua interceptor
        entry.IsDeleted = true; // để RecalcTotals bỏ qua ngay trong cùng unit of work
        FoodLogSupport.RecalcTotals(entry.FoodLog);
        NutritionDataSupport.ClearStaleIncomplete(entry.FoodLog, NutritionDataSupport.Live(entry.FoodLog));   // xoá món cuối ⇒ ngày trống thật ⇒ bỏ cờ chưa-đủ cũ (R2-4), cùng SaveChanges
        await FoodLogSupport.SaveAndRebuildAsync(_uow, _userContext.UserId, entry.FoodLog.LogDate, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}
