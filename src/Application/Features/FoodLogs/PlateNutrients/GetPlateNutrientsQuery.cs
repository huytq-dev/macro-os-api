namespace Application;

/// <summary>
/// Một dòng của plate. Món: FoodItemId + FoodServingId (tuỳ chọn) + Quantity; FoodServingId null thì Quantity là số gram, đúng như log thật (POST /food-logs/entries).
/// Recipe: RecipeId + Quantity (số khẩu phần), không có FoodServingId. Ước lượng/quick add (không có món): Name + tổng kcal/P/C/F, không có vi chất.
/// </summary>
public sealed record PlateLine(
    Guid? FoodItemId,
    Guid? RecipeId,
    Guid? FoodServingId,
    decimal Quantity = 1,
    string? Name = null,
    decimal? Calories = null,
    decimal? Protein = null,
    decimal? Carbs = null,
    decimal? Fat = null
);

/// <summary>Xem trước dinh dưỡng của plate chưa log và của ngày nếu log plate này. Chỉ đọc, không ghi DB; Items rỗng hợp lệ.</summary>
public sealed record GetPlateNutrientsQuery(DateOnly? Date, List<PlateLine>? Items) : IRequest<PlateNutrientsResponse>;

public sealed class PlateLineValidator : AbstractValidator<PlateLine>
{
    public PlateLineValidator()
    {
        RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000)
            .When(x => x.FoodItemId is not null || x.RecipeId is not null);
        RuleFor(x => x.FoodServingId).Null().WithMessage("Recipe không có khẩu phần riêng (FoodServingId phải để trống).")
            .When(x => x.RecipeId is not null);
        RuleFor(x => x.Name).MaximumLength(256).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Calories).InclusiveBetween(0, 10000).When(x => x.Calories is not null);
        RuleFor(x => x.Protein).InclusiveBetween(0, 1000).When(x => x.Protein is not null);
        RuleFor(x => x.Carbs).InclusiveBetween(0, 1000).When(x => x.Carbs is not null);
        RuleFor(x => x.Fat).InclusiveBetween(0, 1000).When(x => x.Fat is not null);
    }
}

public sealed class GetPlateNutrientsQueryValidator : AbstractValidator<GetPlateNutrientsQuery>
{
    public const int MaxLines = 50;

    public GetPlateNutrientsQueryValidator()
    {
        RuleFor(x => x.Items).Must(i => i is null || i.Count <= MaxLines).WithMessage($"At most {MaxLines} items");
        RuleForEach(x => x.Items).SetValidator(new PlateLineValidator());
    }
}

public sealed class GetPlateNutrientsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetPlateNutrientsQuery, PlateNutrientsResponse>
{
    public async Task<PlateNutrientsResponse> Handle(GetPlateNutrientsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);
        var lines = request.Items ?? [];

        // Dòng có món/recipe nạp một lượt cho cả plate; dòng không có món là ước lượng/quick add
        var real = lines.Where(l => l.FoodItemId is not null || l.RecipeId is not null).ToList();
        var lookup = real.Count == 0
            ? null
            : await FoodLogEntryFactory.LoadAsync(_uow, userId, real.Select(l => new EntryLine(l.FoodItemId, l.RecipeId, l.FoodServingId, l.Quantity)).ToList(), ct);

        var parts = new List<IReadOnlyDictionary<string, decimal>>(lines.Count);
        foreach (var l in lines)
        {
            if (l.RecipeId is { } recipeId)
                parts.Add(NutritionCalculator.SnapshotAmounts(lookup!.RecipeProfiles[recipeId], l.Quantity));
            else if (l.FoodItemId is { } foodId)
            {
                var food = lookup!.Foods[foodId];
                var grams = FoodLogSupport.Grams(l.Quantity, FoodLogEntryFactory.ServingOf(food, l.FoodServingId));
                parts.Add(NutritionCalculator.SnapshotAmounts(lookup.FoodProfiles[foodId], NutritionCalculator.GramFactor(grams)));
            }
            else
                parts.Add(new Dictionary<string, decimal>
                {
                    ["energy_kcal"] = NutritionRounding.Round(l.Calories ?? 0, NutritionRounding.Macro),
                    ["protein_g"] = NutritionRounding.Round(l.Protein ?? 0, NutritionRounding.Macro),
                    ["carbs_g"] = NutritionRounding.Round(l.Carbs ?? 0, NutritionRounding.Macro),
                    ["fat_g"] = NutritionRounding.Round(l.Fat ?? 0, NutritionRounding.Macro),
                });
        }
        var plate = NutritionCalculator.SumAmounts(parts);

        // Ngày = đã log (snapshot entry) + plate; chất cả hai đều chưa có dữ liệu thì vắng mặt (amount null)
        var logged = await NutrientCatalog.SumsAsync(_uow, userId, date, date, ct);
        var day = new Dictionary<string, decimal>();
        foreach (var code in logged.Keys.Concat(plate.Keys).Distinct())
        {
            var loggedAmount = logged.GetValueOrDefault(code);
            if (loggedAmount is null && !plate.ContainsKey(code)) continue;
            day[code] = (loggedAmount ?? 0m) + plate.GetValueOrDefault(code);
        }

        return new PlateNutrientsResponse(
            date,
            await NutrientBreakdown.BuildAsync(_uow, userId, date, plate, ct),
            await NutrientBreakdown.BuildAsync(_uow, userId, date, day, ct));
    }
}
