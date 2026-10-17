namespace Application;

/// <summary>UC-D03: sửa bữa/giờ/serving/số lượng; snapshot tính lại từ FoodItem hiện tại.</summary>
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
        if (entry.FoodItemId is not { } foodId)
            throw new BadRequestException("Entry từ recipe chưa hỗ trợ sửa (MOS-009).");

        var food = await FoodLogSupport.GetLoggableFoodAsync(_uow, userId, foodId, ct);
        var serving = request.FoodServingId is { } sid
            ? food.Servings.FirstOrDefault(s => s.Id == sid) ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodServing", sid))
            : null;

        // Không gửi giờ → giữ giờ cũ (không xoá); không gửi bữa → bữa suy từ giờ mới nếu có đổi giờ, ngược lại giữ bữa cũ
        var eatenAt = request.EatenAt ?? entry.EatenAt ?? MealTimes.Default(entry.MealType);
        entry.EatenAt = eatenAt;
        entry.MealType = request.MealType ?? (request.EatenAt is not null ? MealTimes.ForTime(eatenAt) : entry.MealType);
        entry.FoodServingId = serving?.Id;
        entry.Quantity = request.Quantity;
        var grams = FoodLogSupport.Grams(request.Quantity, serving);
        FoodLogSupport.Snapshot(entry, food, grams);
        FoodLogSupport.RecalcTotals(entry.FoodLog);
        await NutrientSnapshot.WriteAsync(_uow, entry.Id, food, grams, ct);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, entry.FoodLog.LogDate, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}
