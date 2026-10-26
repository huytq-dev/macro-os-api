namespace Application;

public sealed class DeleteNutritionDataCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<DeleteNutritionDataCommand, Unit>
{
    public async Task<Unit> Handle(DeleteNutritionDataCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await _uow.Repository<FoodLog>().Query(tracking: true).Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == request.Date, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLog", request.Date));

        // Phạm vi: dữ liệu NDM của ngày = manual override (4 cột) + cờ nhịn ăn. KHÔNG xoá dòng FoodLog, món đã log, nước, ghi chú, cờ chưa đủ (trừ khi ngày thành trống thật: R2-4).
        var live = NutritionDataSupport.Live(log);
        var changed = false;
        if (!NutritionDataSupport.HasManual(log) && !log.IsFasting)
        {
            // Không có dữ liệu NDM để xoá: ngày chỉ có món → 400 như cũ (xoá món ở Food Log); ngày trống chỉ còn cờ chưa-đủ cũ → chỉ dọn cờ đó
            if (live.Count > 0) throw new BadRequestException(ExceptionMessages.DayLoggedInFoodLogDelete);
        }
        else
        {
            changed |= NutritionDataSupport.ClearManual(log);
            if (log.IsFasting) { log.IsFasting = false; changed = true; }
        }

        changed |= NutritionDataSupport.ClearStaleIncomplete(log, live);
        if (changed) await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.Date, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}
