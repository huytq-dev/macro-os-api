namespace Application;

/// <summary>
/// UC-D03: sửa bữa/giờ/serving/số lượng. Entry món: snapshot tính lại từ FoodItem hiện tại.
/// Entry recipe: chỉ đổi số khẩu phần (Quantity, FoodServingId để trống); snapshot cũ nhân theo tỉ lệ, không đọc lại recipe.
/// </summary>
public sealed record UpdateFoodLogEntryCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    MealType? MealType,
    TimeOnly? EatenAt,
    Guid? FoodServingId,
    decimal Quantity
) : IRequest<Unit>;

public sealed class UpdateFoodLogEntryCommandValidator : AbstractValidator<UpdateFoodLogEntryCommand>
{
    public UpdateFoodLogEntryCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
    }
}

public sealed class UpdateFoodLogEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<UpdateFoodLogEntryCommand, Unit>
{
    public async Task<Unit> Handle(UpdateFoodLogEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var entry = await FoodLogSupport.GetOwnedEntryAsync(_uow, userId, request.Id, ct);

        // Không gửi giờ → giữ giờ cũ (không xoá); không gửi bữa → bữa suy từ giờ mới nếu có đổi giờ, ngược lại giữ bữa cũ
        var eatenAt = request.EatenAt ?? entry.EatenAt ?? MealTimes.Default(entry.MealType);
        var oldQuantity = entry.Quantity;
        entry.EatenAt = eatenAt;
        entry.MealType = request.MealType ?? (request.EatenAt is not null ? MealTimes.ForTime(eatenAt) : entry.MealType);
        entry.Quantity = request.Quantity;

        if (entry.FoodItemId is { } foodId)
        {
            var lookup = await FoodLogEntryFactory.LoadAsync(_uow, userId, [new EntryLine(foodId, null, request.FoodServingId, request.Quantity)], ct);
            var food = lookup.Foods[foodId];
            var serving = FoodLogEntryFactory.ServingOf(food, request.FoodServingId);
            entry.FoodServingId = serving?.Id;
            var grams = FoodLogSupport.Grams(request.Quantity, serving);
            FoodLogSupport.Snapshot(entry, food, grams, lookup.FoodProfiles[foodId]);
            await NutrientSnapshot.WriteAsync(_uow, entry.Id, lookup.FoodProfiles[foodId], NutritionCalculator.GramFactor(grams), lookup.CodeIds, ct);
        }
        else
        {
            if (request.FoodServingId is not null) throw new BadRequestException("Recipe không có khẩu phần riêng (FoodServingId phải để trống).");
            var ratio = request.Quantity / oldQuantity;
            entry.Calories = NutritionCalculator.Rescale(entry.Calories, ratio);
            entry.Protein = NutritionCalculator.Rescale(entry.Protein, ratio);
            entry.Carbs = NutritionCalculator.Rescale(entry.Carbs, ratio);
            entry.Fat = NutritionCalculator.Rescale(entry.Fat, ratio);
            entry.Fiber = NutritionCalculator.Rescale(entry.Fiber, ratio);
            entry.Sodium = NutritionCalculator.Rescale(entry.Sodium, ratio);
            await NutrientSnapshot.ScaleAsync(_uow, entry.Id, ratio, ct);
        }
        FoodLogSupport.RecalcTotals(entry.FoodLog);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, entry.FoodLog.LogDate, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}
