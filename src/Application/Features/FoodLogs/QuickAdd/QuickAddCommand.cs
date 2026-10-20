namespace Application;

/// <summary>UC-D04: nhập thẳng kcal/P/C/F. Tạo FoodItem ẩn Source=QuickAdd (giá trị = /100 g) và entry 100 g.</summary>
public sealed record QuickAddCommand(
    DateOnly? LogDate,
    MealType? MealType,
    TimeOnly? EatenAt,
    string? Name,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat
) : IRequest<Guid>;

public sealed class QuickAddCommandValidator : AbstractValidator<QuickAddCommand>
{
    public QuickAddCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Name).MaximumLength(256).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Calories).InclusiveBetween(0, 10000);
        RuleFor(x => x.Protein).InclusiveBetween(0, 1000);
        RuleFor(x => x.Carbs).InclusiveBetween(0, 1000);
        RuleFor(x => x.Fat).InclusiveBetween(0, 1000);
    }
}

public sealed class QuickAddCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<QuickAddCommand, Guid>
{
    public async Task<Guid> Handle(QuickAddCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;

        var food = new FoodItem
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Quick add" : request.Name.Trim(),
            Category = FoodCategory.Other,
            Source = FoodSource.QuickAdd,
            CreatedByUserId = userId,
        };
        FoodNutritionApplier.Apply(food, new FoodNutrition(request.Calories, request.Protein, request.Carbs, request.Fat));
        await _uow.Repository<FoodItem>().AddAsync(food, ct);

        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.LogDate ?? FoodLogSupport.Today(_timeProvider), ct);
        var (eatenAt, meal) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, log.LogDate, request.EatenAt, request.MealType, _timeProvider, ct);
        var entry = new FoodLogEntry
        {
            Id = Guid.NewGuid(),
            FoodLogId = log.Id,
            MealType = meal,
            EatenAt = eatenAt,
            FoodItemId = food.Id,
            Quantity = 100, // gram; /100 g × 100 g = đúng giá trị nhập
        };
        FoodLogSupport.Snapshot(entry, food, 100);
        await FoodLogSupport.AddEntryAsync(_uow, log, entry, ct);
        FoodLogSupport.RecalcTotals(log);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);
        return entry.Id;
    }
}
