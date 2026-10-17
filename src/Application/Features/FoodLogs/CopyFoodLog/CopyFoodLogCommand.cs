namespace Application;

/// <summary>UC-D05: copy bữa (MealType) hoặc cả ngày (MealType null) sang ngày khác; snapshot copy nguyên, không đọc lại FoodItem.</summary>
public sealed record CopyFoodLogCommand(
    DateOnly FromDate,
    DateOnly ToDate,
    MealType? MealType,
    MealType? ToMealType
) : IRequest<int>;

public sealed class CopyFoodLogCommandValidator : AbstractValidator<CopyFoodLogCommand>
{
    public CopyFoodLogCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.MealType.HasValue);
        RuleFor(x => x.ToMealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.ToMealType.HasValue);
        RuleFor(x => x).Must(x => x.FromDate != x.ToDate || (x.MealType.HasValue && x.ToMealType.HasValue && x.MealType != x.ToMealType))
            .WithMessage("Nguồn và đích trùng nhau.");
    }
}

public sealed class CopyFoodLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<CopyFoodLogCommand, int>
{
    public async Task<int> Handle(CopyFoodLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;

        var source = await _uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodLog.UserId == userId && e.FoodLog.LogDate == request.FromDate)
            .Where(e => request.MealType == null || e.MealType == request.MealType)
            .ToListAsync(ct);
        if (source.Count == 0) return 0;

        var target = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.ToDate, ct);
        var copied = new Dictionary<Guid, Guid>(); // entry nguồn → entry đích, để copy snapshot nutrient
        foreach (var s in source)
        {
            var copy = new FoodLogEntry
            {
                Id = Guid.NewGuid(),
                FoodLogId = target.Id,
                MealType = request.ToMealType ?? s.MealType,
                EatenAt = s.EatenAt,
                FoodItemId = s.FoodItemId,
                RecipeId = s.RecipeId,
                FoodServingId = s.FoodServingId,
                Quantity = s.Quantity,
                Calories = s.Calories, Protein = s.Protein, Carbs = s.Carbs, Fat = s.Fat,
                Fiber = s.Fiber, Sodium = s.Sodium,
            };
            target.Entries.Add(copy);
            copied[s.Id] = copy.Id;
        }
        await NutrientSnapshot.CopyAsync(_uow, copied, ct);
        FoodLogSupport.RecalcTotals(target);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.ToDate, _options.Value, _timeProvider, ct);
        return source.Count;
    }
}
