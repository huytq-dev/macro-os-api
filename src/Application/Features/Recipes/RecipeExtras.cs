using System.Text.Json;

namespace Application;

/// <summary>Quy tắc cho phần "Preparation (optional)" của recipe: link, các bước làm, icon.</summary>
internal static class RecipePreparation
{
    public const int DescriptionMaxLength = 1500;
    public const int LinkMaxLength = 500;
    public const int MaxSteps = 50;
    public const int StepMaxLength = 300;

    public static bool IsHttpUrl(string? text)
        => Uri.TryCreate(text?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public static void Rules<T>(AbstractValidator<T> v, Expression<Func<T, IEnumerable<RecipeStepInput>>> steps, Expression<Func<T, string?>> iconKey)
    {
        v.RuleFor(steps).Must(s => s is null || s.Count() <= MaxSteps).WithMessage(ValidationMessages.RecipeStepsMax);
        v.RuleForEach(steps).ChildRules(s => s.RuleFor(x => x.Text).NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(StepMaxLength).WithMessage(ValidationMessages.MaxLength));
        v.RuleFor(iconKey).Must(FoodIconKeys.IsValid).When(x => iconKey.Compile()(x) is not null).WithMessage(ValidationMessages.InvalidIconKey);
    }
}

/// <summary>Các bước làm lưu dạng JSON một cột trên Recipe (luôn đọc/ghi cả danh sách, không truy vấn từng bước).</summary>
internal static class RecipeSteps
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Stored(int Order, string Text);

    /// <summary>Sắp theo Order (thiếu thì theo vị trí trong mảng), bỏ bước rỗng, đánh số lại 1..n; không còn bước nào → null.</summary>
    public static string? Serialize(IReadOnlyList<RecipeStepInput>? steps)
    {
        if (steps is null) return null;
        var stored = steps
            .Select((s, i) => (Key: s.Order ?? i + 1, Index: i, Text: s.Text.Trim()))
            .Where(s => s.Text.Length > 0)
            .OrderBy(s => s.Key).ThenBy(s => s.Index)
            .Select((s, i) => new Stored(i + 1, s.Text))
            .ToList();
        return stored.Count == 0 ? null : JsonSerializer.Serialize(stored, Json);
    }

    public static List<RecipeStepModel> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return (JsonSerializer.Deserialize<List<Stored>>(json, Json) ?? []).Select(s => new RecipeStepModel(s.Order, s.Text)).ToList(); }
        catch (JsonException) { return []; }
    }
}

/// <summary>
/// Create Recipe step 1: dinh dưỡng của recipe đang soạn (chưa lưu). Nguyên liệu như lúc tạo: FoodServingId null = Quantity là gram.
/// Trả tổng và theo khẩu phần (tổng ÷ ServingsCount), mỗi bên đủ danh mục chất kèm % target của ngày <c>Date</c> (mặc định hôm nay theo múi giờ user). Chỉ đọc.
/// </summary>
public sealed record GetRecipeNutrientsQuery(List<RecipeIngredientInput> Ingredients, int ServingsCount = 1, DateOnly? Date = null) : IRequest<RecipeNutrientsResponse>;

public sealed class GetRecipeNutrientsQueryValidator : AbstractValidator<GetRecipeNutrientsQuery>
{
    public GetRecipeNutrientsQueryValidator()
    {
        RuleFor(x => x.ServingsCount).InclusiveBetween(1, 100);
        RuleFor(x => x.Ingredients).NotEmpty().WithMessage(ValidationMessages.Required)
            .Must(l => l is null || l.Count <= 100).WithMessage("Tối đa 100 nguyên liệu.");
        RuleForEach(x => x.Ingredients).ChildRules(i =>
        {
            i.RuleFor(x => x.FoodItemId).NotEmpty().WithMessage(ValidationMessages.Required);
            i.RuleFor(x => x.FoodServingId).Must(id => id is null || id != Guid.Empty).WithMessage(ValidationMessages.Required);
            i.RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
        });
    }
}

public sealed class GetRecipeNutrientsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetRecipeNutrientsQuery, RecipeNutrientsResponse>
{
    public async Task<RecipeNutrientsResponse> Handle(GetRecipeNutrientsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var lookup = await FoodLogEntryFactory.LoadAsync(_uow, userId, request.Ingredients.Select(i => new EntryLine(i.FoodItemId, null, i.FoodServingId, i.Quantity)).ToList(), ct);

        decimal totalGrams = 0;
        var parts = new List<IReadOnlyDictionary<string, decimal>>(request.Ingredients.Count);
        foreach (var i in request.Ingredients)
        {
            var food = lookup.Foods[i.FoodItemId];
            var grams = FoodLogSupport.Grams(i.Quantity, FoodLogEntryFactory.ServingOf(food, i.FoodServingId));
            totalGrams += grams;
            parts.Add(NutritionCalculator.SnapshotAmounts(lookup.FoodProfiles[food.Id], NutritionCalculator.GramFactor(grams)));
        }
        var total = NutritionCalculator.SumAmounts(parts);
        var perServing = total.ToDictionary(kv => kv.Key, kv => kv.Value / request.ServingsCount);

        var date = request.Date ?? (await FoodLogSupport.LocalNowAsync(_uow, userId, _timeProvider, ct)).Date;
        return new RecipeNutrientsResponse(
            date, NutritionRounding.Round(totalGrams, NutritionRounding.Grams), request.ServingsCount,
            Macros(total), Macros(perServing),
            await NutrientBreakdown.BuildAsync(_uow, userId, date, total, ct),
            await NutrientBreakdown.BuildAsync(_uow, userId, date, perServing, ct));
    }

    private static RecipeMacros Macros(IReadOnlyDictionary<string, decimal> amounts) => new(
        NutritionRounding.Round(amounts.GetValueOrDefault("energy_kcal"), NutritionRounding.Macro),
        NutritionRounding.Round(amounts.GetValueOrDefault("protein_g"), NutritionRounding.Macro),
        NutritionRounding.Round(amounts.GetValueOrDefault("carbs_g"), NutritionRounding.Macro),
        NutritionRounding.Round(amounts.GetValueOrDefault("fat_g"), NutritionRounding.Macro));
}

/// <summary>Log recipe vừa tạo: ngày/bữa/giờ như POST /food-logs/entries (thiếu → hôm nay, giờ hiện tại của user), Quantity = số khẩu phần ăn.</summary>
public sealed record RecipeLogOptions(DateOnly? LogDate, MealType? MealType, TimeOnly? EatenAt, decimal Quantity = 1);

/// <summary>"Create &amp; Add": tạo recipe rồi log nó (một lời gọi). Hai bước là hai transaction: nếu log lỗi thì recipe vẫn được tạo (lỗi trả về, FE gọi lại POST /food-logs/entries với recipeId).</summary>
public sealed record CreateAndLogRecipeCommand(CreateRecipeCommand Recipe, RecipeLogOptions? Log = null) : IRequest<RecipeCreatedResponse>;

public sealed class CreateAndLogRecipeCommandValidator : AbstractValidator<CreateAndLogRecipeCommand>
{
    public CreateAndLogRecipeCommandValidator()
    {
        RuleFor(x => x.Recipe).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Log!.Quantity).InclusiveBetween(0.01m, 10000).When(x => x.Log is not null);
    }
}

public sealed class CreateAndLogRecipeCommandHandler(ISender _sender) : IRequestHandler<CreateAndLogRecipeCommand, RecipeCreatedResponse>
{
    public async Task<RecipeCreatedResponse> Handle(CreateAndLogRecipeCommand request, CancellationToken ct)
    {
        var recipeId = await _sender.Send(request.Recipe, ct); // qua pipeline: validate + lưu
        var log = request.Log ?? new RecipeLogOptions(null, null, null);
        var entryId = await _sender.Send(new AddFoodLogEntryCommand(log.LogDate, log.MealType, log.EatenAt, null, null, log.Quantity, recipeId), ct);
        return new RecipeCreatedResponse(recipeId, entryId);
    }
}
