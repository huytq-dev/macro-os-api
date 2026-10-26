namespace Application;

public sealed class ClearDayLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<ClearDayLogCommand, Unit>
{
    public async Task<Unit> Handle(ClearDayLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await _uow.Repository<FoodLog>().Query(tracking: true).Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == request.Date, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLog", request.Date));

        // Phạm vi: các MÓN Food Log của ngày (mọi entry, kể cả Quick Add). Giữ: dòng FoodLog, Manual Nutrition (override), nước, ghi chú, cờ nhịn ăn. Cờ chưa-đủ chỉ tự bỏ khi ngày thành trống thật (R2-4), cùng SaveChanges.
        // Không có món nào ⇒ không có gì để xoá: thành công, không ghi (idempotent: bấm lần hai không lỗi)
        var foods = NutritionDataSupport.Live(log);
        if (foods.Count == 0) return Unit.Value;

        await NutritionDataSupport.SoftDeleteAsync(_uow, foods, ct);
        FoodLogSupport.RecalcTotals(log);
        NutritionDataSupport.ClearStaleIncomplete(log, NutritionDataSupport.Live(log));
        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.Date, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}
