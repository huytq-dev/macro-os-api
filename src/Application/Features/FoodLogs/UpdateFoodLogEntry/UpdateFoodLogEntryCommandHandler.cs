namespace Application;

public sealed class UpdateFoodLogEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<UpdateFoodLogEntryCommand, Unit>
{
    public async Task<Unit> Handle(UpdateFoodLogEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var entry = await FoodLogSupport.GetOwnedEntryAsync(_uow, userId, request.Id, ct);

        if (entry.FoodItemId is null && request.FoodServingId is not null) throw new BadRequestException(ValidationMessages.RecipeHasNoOwnServing);

        // Chỉ khi ĐỔI khẩu phần và entry có snapshot gốc mới cần gram của khẩu phần MỚI (lựa chọn mới của user); entry cũ không snapshot bị Plan từ chối trước (LEGACY_SERVING_SNAPSHOT_MISSING, không ghi gì);
        // đổi bữa/giờ/số lượng không đọc FoodItem/Recipe
        ServingRef? newServing = null;
        if (entry.FoodItemId is { } foodId && request.FoodServingId is { } sid && sid != entry.FoodServingId && EntryServingSnapshot.Parse(entry.ServingSnapshotJson) is not null)
        {
            var s = await _uow.Repository<FoodServing>().FindAsync(x => x.Id == sid && x.FoodItemId == foodId, ct)
                ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodServing", sid));
            newServing = new ServingRef(s.Id, s.ServingName, s.WeightInGrams);
        }
        var plan = EntryEdit.Plan(entry, request.Quantity, request.FoodServingId, newServing);

        // Không gửi giờ → giữ giờ cũ (không xoá); không gửi bữa → bữa suy từ giờ mới nếu có đổi giờ, ngược lại giữ bữa cũ
        var eatenAt = request.EatenAt ?? entry.EatenAt ?? MealTimes.Default(entry.MealType);
        entry.EatenAt = eatenAt;
        entry.MealType = request.MealType ?? (request.EatenAt is not null ? MealTimes.ForTime(eatenAt) : entry.MealType);

        if (!plan.Unchanged)
        {
            entry.Quantity = request.Quantity;
            entry.FoodServingId = request.FoodServingId;
            entry.Calories = plan.Macros!.Calories;
            entry.Protein = plan.Macros.Protein;
            entry.Carbs = plan.Macros.Carbs;
            entry.Fat = plan.Macros.Fat;
            entry.Fiber = plan.Macros.Fiber;
            entry.Sodium = plan.Macros.Sodium;
            entry.ServingSnapshotJson = plan.SnapshotJson;
            await NutrientSnapshot.ScaleAsync(_uow, entry.Id, plan.Ratio, ct);
            FoodLogSupport.RecalcTotals(entry.FoodLog);
        }

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, entry.FoodLog.LogDate, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}
