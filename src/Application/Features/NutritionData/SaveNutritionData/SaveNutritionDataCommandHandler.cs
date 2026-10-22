namespace Application;

public sealed class SaveNutritionDataCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<SaveNutritionDataCommand, NutritionDataDayModel>
{
    public async Task<NutritionDataDayModel> Handle(SaveNutritionDataCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date!.Value;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        locale.EnsureNotFuture(date, "Date");

        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, date, ct);
        var live = log.Entries.Where(e => !e.IsDeleted).ToList();
        // Ngày có món log từ Food Log (không phải Quick Add): không được ghi đè/xoá món ở đây — chỉ đổi được cờ IsIncomplete
        if (NutritionDataSupport.IsAppDay(live) && (request.IsFasting || request.Calories is not null))
            throw new BadRequestException(ExceptionMessages.DayLoggedInFoodLog);

        var replace = request.IsFasting || request.Calories is not null;
        var (wasFasting, wasIncomplete, liveBefore) = (log.IsFasting, log.IsIncomplete, live.Count);

        if (!replace && live.Count == 0 && !log.IsFasting)
            throw new BadRequestException(ExceptionMessages.NutritionDataNeedsEnergy);

        if (replace)
        {
            foreach (var old in live)
            {
                await _uow.Repository<FoodLogEntry>().RemoveAsync(old, ct); // soft delete qua interceptor
                old.IsDeleted = true;
            }
            live.Clear();

            if (request.IsFasting) log.IsFasting = true;
            else
            {
                var food = new FoodItem { Id = Guid.NewGuid(), Name = "Manual entry", Category = FoodCategory.Other, Source = FoodSource.QuickAdd, CreatedByUserId = userId };
                FoodNutritionApplier.Apply(food, new FoodNutrition(request.Calories!.Value, request.ProteinG ?? 0, request.CarbsG ?? 0, request.FatG ?? 0));
                await _uow.Repository<FoodItem>().AddAsync(food, ct);

                var (eatenAt, meal) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, log.LogDate, null, null, _timeProvider, ct);
                var entry = new FoodLogEntry
                {
                    Id = Guid.NewGuid(), FoodLogId = log.Id, MealType = meal, EatenAt = eatenAt, FoodItemId = food.Id,
                    Quantity = 100, // gram; /100 g × 100 g = đúng giá trị nhập
                    Source = EntrySource.QuickAdd,
                };
                FoodLogSupport.Snapshot(entry, food, 100);
                EntryServingSnapshot.Stamp(entry, null, null, null, 100);
                await FoodLogSupport.AddEntryAsync(_uow, log, entry, ct); // tắt cờ fasting
                live.Add(entry);
            }
        }
        else if (log.IsFasting && live.Count == 0) log.IsFasting = false; // bỏ nhịn ăn mà không nhập số: ngày thành chưa log

        log.IsIncomplete = request.IsIncomplete;
        FoodLogSupport.RecalcTotals(log);
        // Lưu lại đúng trạng thái cũ (nhịn ăn → nhịn ăn, cờ không đổi) không có gì để ghi: EnsureSaveAsync sẽ ném lỗi, nên bỏ qua
        var changed = (replace && (!request.IsFasting || liveBefore > 0)) || log.IsFasting != wasFasting || log.IsIncomplete != wasIncomplete;
        if (changed) await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);

        var fasting = log.IsFasting && live.Count == 0;
        return new NutritionDataDayModel(log.LogDate, fasting ? 0 : log.TotalCalories, fasting ? 0 : log.TotalProtein, fasting ? 0 : log.TotalFat, fasting ? 0 : log.TotalCarbs,
            NutritionDataSupport.SourceOf(fasting, live.Count, live.Count(e => e.Source == EntrySource.QuickAdd)), fasting, log.IsIncomplete, live.Count);
    }
}
