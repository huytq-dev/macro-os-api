namespace Application;

/// <summary>UC-D02: thêm entry. Quantity = số serving (có FoodServingId) hoặc số gram (không có).</summary>
public sealed record AddFoodLogEntryCommand(
    DateOnly? LogDate,
    MealType MealType,
    TimeOnly? EatenAt,
    Guid FoodItemId,
    Guid? FoodServingId,
    decimal Quantity
) : IRequest<Guid>;

public sealed class AddFoodLogEntryCommandValidator : AbstractValidator<AddFoodLogEntryCommand>
{
    public AddFoodLogEntryCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.FoodItemId).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
    }
}

public sealed class AddFoodLogEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<AddFoodLogEntryCommand, Guid>
{
    public async Task<Guid> Handle(AddFoodLogEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var food = await FoodLogSupport.GetLoggableFoodAsync(_uow, userId, request.FoodItemId, ct);
        var serving = request.FoodServingId is { } sid
            ? food.Servings.FirstOrDefault(s => s.Id == sid) ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodServing", sid))
            : null;

        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.LogDate ?? FoodLogSupport.Today(_timeProvider), ct);

        var entry = new FoodLogEntry
        {
            Id = Guid.NewGuid(),
            FoodLogId = log.Id,
            MealType = request.MealType,
            EatenAt = request.EatenAt,
            FoodItemId = food.Id,
            FoodServingId = serving?.Id,
            Quantity = request.Quantity,
        };
        FoodLogSupport.Snapshot(entry, food, FoodLogSupport.Grams(request.Quantity, serving));
        log.Entries.Add(entry);
        FoodLogSupport.RecalcTotals(log);

        await _uow.EnsureSaveAsync(ct);
        return entry.Id;
    }
}
