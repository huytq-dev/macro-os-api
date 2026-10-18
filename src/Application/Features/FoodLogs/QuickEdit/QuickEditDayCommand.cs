namespace Application;

/// <summary>
/// WP6 / UC-D14: Quick Edit ngày — thay toàn bộ entry của ngày bằng đúng một quick add (kcal/P/C/F nhập tay). Entry cũ bị xoá mềm.
/// MealType/EatenAt thiếu → suy như quick add thường.
/// </summary>
public sealed record QuickEditDayCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] DateOnly Date,
    string? Name,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    MealType? MealType,
    TimeOnly? EatenAt
) : IRequest<Guid>;

public sealed class QuickEditDayCommandValidator : AbstractValidator<QuickEditDayCommand>
{
    public QuickEditDayCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Name).MaximumLength(256).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Calories).InclusiveBetween(0, 10000);
        RuleFor(x => x.Protein).InclusiveBetween(0, 1000);
        RuleFor(x => x.Carbs).InclusiveBetween(0, 1000);
        RuleFor(x => x.Fat).InclusiveBetween(0, 1000);
    }
}

public sealed class QuickEditDayCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<QuickEditDayCommand, Guid>
{
    public async Task<Guid> Handle(QuickEditDayCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.Date, ct);

        foreach (var old in log.Entries.Where(e => !e.IsDeleted).ToList())
        {
            await _uow.Repository<FoodLogEntry>().RemoveAsync(old, ct); // soft delete qua interceptor
            old.IsDeleted = true;
        }

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

        var (eatenAt, meal) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, log.LogDate, request.EatenAt, request.MealType, _timeProvider, ct);
        var entry = new FoodLogEntry
        {
            Id = Guid.NewGuid(),
            FoodLogId = log.Id,
            MealType = meal,
            EatenAt = eatenAt,
            FoodItemId = food.Id,
            Quantity = 100, // gram; /100 g × 100 g = đúng giá trị nhập
            Source = EntrySource.QuickAdd,
        };
        FoodLogSupport.Snapshot(entry, food, 100);
        log.Entries.Add(entry);
        FoodLogSupport.RecalcTotals(log);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);
        return entry.Id;
    }
}
