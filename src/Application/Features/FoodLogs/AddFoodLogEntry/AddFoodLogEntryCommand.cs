namespace Application;

/// <summary>UC-D02: thêm entry. Món (FoodItemId): Quantity = số serving (có FoodServingId) hoặc số gram (không có). Recipe (RecipeId): Quantity = số khẩu phần, không có FoodServingId.</summary>
public sealed record AddFoodLogEntryCommand(
    DateOnly? LogDate,
    MealType? MealType,
    TimeOnly? EatenAt,
    Guid? FoodItemId,
    Guid? FoodServingId,
    decimal Quantity,
    Guid? RecipeId = null
) : IRequest<Guid>;

public sealed class AddFoodLogEntryCommandValidator : AbstractValidator<AddFoodLogEntryCommand>
{
    public AddFoodLogEntryCommandValidator()
    {
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x).Must(x => x.FoodItemId.HasValue != x.RecipeId.HasValue)
            .WithName("FoodItemId").WithMessage("Cần đúng một trong FoodItemId hoặc RecipeId.");
        RuleFor(x => x.FoodServingId).Null().When(x => x.RecipeId.HasValue).WithMessage("Recipe không có khẩu phần riêng.");
        RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
    }
}

public sealed class AddFoodLogEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<AddFoodLogEntryCommand, Guid>
{
    public async Task<Guid> Handle(AddFoodLogEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var line = new EntryLine(request.FoodItemId, request.RecipeId, request.FoodServingId, request.Quantity);
        var lookup = await FoodLogEntryFactory.LoadAsync(_uow, userId, [line], ct);

        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.LogDate ?? FoodLogSupport.Today(_timeProvider), ct);
        var (eatenAt, meal) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, log.LogDate, request.EatenAt, request.MealType, _timeProvider, ct);

        var entry = await FoodLogEntryFactory.AddAsync(_uow, lookup, line, log, eatenAt, meal, batchId: null, ct);
        FoodLogSupport.RecalcTotals(log);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);
        return entry.Id;
    }
}
