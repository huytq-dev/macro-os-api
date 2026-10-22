using System.Text.Json;

namespace Application;

/// <summary>Quy tắc kiểm tra chung Create/Update recipe và dựng nguyên liệu + bản (RecipeVersion).</summary>
internal static class RecipeWriter
{
    public static void Rules<T>(AbstractValidator<T> v,
        Expression<Func<T, string>> name, Expression<Func<T, string?>> description, Expression<Func<T, string?>> imageUrl,
        Expression<Func<T, int>> servings, Expression<Func<T, int?>> prep, Expression<Func<T, int?>> cook,
        Expression<Func<T, MealType?>> meal, Expression<Func<T, string?>> sourceUrl, Expression<Func<T, string?>> instructions,
        Expression<Func<T, decimal?>> cookedYield, Expression<Func<T, IEnumerable<RecipeIngredientInput>>> ingredients)
    {
        v.RuleFor(name).NotEmpty().WithMessage(ValidationMessages.Required).MaximumLength(256).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(description).MaximumLength(RecipePreparation.DescriptionMaxLength).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(imageUrl).MaximumLength(1024).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(servings).InclusiveBetween(1, 100);
        v.RuleFor(prep).InclusiveBetween(0, 1440).When(x => prep.Compile()(x).HasValue);
        v.RuleFor(cook).InclusiveBetween(0, 1440).When(x => cook.Compile()(x).HasValue);
        v.RuleFor(meal).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => meal.Compile()(x).HasValue);
        v.RuleFor(sourceUrl).MaximumLength(RecipePreparation.LinkMaxLength).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(sourceUrl).Must(RecipePreparation.IsHttpUrl).When(x => !string.IsNullOrWhiteSpace(sourceUrl.Compile()(x))).WithMessage(ValidationMessages.InvalidRecipeLink);
        v.RuleFor(instructions).MaximumLength(4000).WithMessage(ValidationMessages.MaxLength);
        v.RuleFor(cookedYield).InclusiveBetween(0.1m, 100000m).When(x => cookedYield.Compile()(x).HasValue);
        v.RuleFor(ingredients).NotEmpty().WithMessage(ValidationMessages.Required)
            .Must(l => l is null || l.Count() <= 100).WithMessage(ValidationMessages.MaxIngredients100);
        v.RuleForEach(ingredients).ChildRules(i =>
        {
            i.RuleFor(x => x.FoodItemId).NotEmpty().WithMessage(ValidationMessages.Required);
            i.RuleFor(x => x.FoodServingId).Must(id => id is null || id != Guid.Empty).WithMessage(ValidationMessages.Required);
            i.RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
            i.RuleFor(x => x.Notes).MaximumLength(256).WithMessage(ValidationMessages.MaxLength);
        });
    }

    /// <summary>Gán nguyên liệu + tổng macro cho recipe và dựng bản mới (chưa lưu). Món phải là món hệ thống hoặc của chính user.</summary>
    public static async Task<RecipeVersion> ApplyAsync(IUnitOfWork uow, Guid userId, Recipe recipe, int versionNo, string? instructions,
        decimal? cookedYield, IReadOnlyList<RecipeIngredientInput> inputs, CancellationToken ct)
    {
        var foodIds = inputs.Select(i => i.FoodItemId).Distinct().ToList();
        var foods = await uow.Repository<FoodItem>().Query()
            .Include(f => f.Servings)
            .Where(f => foodIds.Contains(f.Id) && (f.CreatedByUserId == null || f.CreatedByUserId == userId))
            .ToDictionaryAsync(f => f.Id, ct);

        decimal cal = 0, protein = 0, carbs = 0, fat = 0, rawGrams = 0;
        var snapshot = new List<object>(inputs.Count);
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            var food = foods.GetValueOrDefault(input.FoodItemId) ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", input.FoodItemId));
            // FoodServingId null: Quantity là số gram → giữ đúng gram, khẩu phần lưu là khẩu phần mặc định (GramsNormalized mới là nguồn tính toán)
            var serving = input.FoodServingId is { } servingId
                ? food.Servings.FirstOrDefault(s => s.Id == servingId) ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodServing", servingId))
                : food.Servings.Where(s => s.WeightInGrams > 0).OrderByDescending(s => s.IsDefault).ThenBy(s => s.ServingName, StringComparer.Ordinal).FirstOrDefault()
                    ?? throw new BadRequestException(ExceptionMessages.FoodHasNoServingAsIngredient(food.Name));

            var grams = input.FoodServingId is null ? NutritionRounding.Round(input.Quantity, NutritionRounding.Grams) : RecipeCalculator.Grams(input.Quantity, serving);
            var quantity = input.FoodServingId is null ? Math.Max(0.01m, Math.Round(grams / serving.WeightInGrams, 2)) : input.Quantity;
            // Cộng chưa làm tròn rồi làm tròn một lần ở cuối (giữ nguyên hành vi cũ của tổng recipe)
            var k = NutritionCalculator.GramFactor(grams);
            cal += food.CaloriesPer100g * k;
            protein += food.ProteinPer100g * k;
            carbs += food.CarbsPer100g * k;
            fat += food.FatPer100g * k;
            rawGrams += grams;

            // Thêm qua repository (trạng thái Added): thêm vào recipe.Ingredients khi recipe đang tracked (sửa recipe) làm EF coi dòng có Id gán sẵn là đã tồn tại
            // (Modified) nên UPDATE 0 dòng và DbUpdateConcurrencyException — cùng lỗi với FoodLogSupport.AddEntryAsync
            await uow.Repository<RecipeIngredient>().AddAsync(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                FoodItemId = food.Id,
                FoodServingId = serving.Id,
                Quantity = quantity,
                SortOrder = i,
                Notes = input.Notes?.Trim(),
                GramsNormalized = grams,
            }, ct);
            snapshot.Add(new
            {
                foodItemId = food.Id, foodName = food.Name, servingId = serving.Id, servingName = serving.ServingName,
                quantity, grams,
                calories = NutritionCalculator.Scale(food.CaloriesPer100g, grams), protein = NutritionCalculator.Scale(food.ProteinPer100g, grams),
                carbs = NutritionCalculator.Scale(food.CarbsPer100g, grams), fat = NutritionCalculator.Scale(food.FatPer100g, grams),
            });
        }

        recipe.TotalCalories = NutritionRounding.Round(cal, NutritionRounding.Macro);
        recipe.TotalProtein = NutritionRounding.Round(protein, NutritionRounding.Macro);
        recipe.TotalCarbs = NutritionRounding.Round(carbs, NutritionRounding.Macro);
        recipe.TotalFat = NutritionRounding.Round(fat, NutritionRounding.Macro);

        return new RecipeVersion
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            VersionNo = versionNo,
            Servings = recipe.ServingsCount,
            CookedYieldGrams = cookedYield,
            RawTotalGrams = NutritionRounding.Round(rawGrams, NutritionRounding.Grams),
            Instructions = instructions?.Trim(),
            IngredientsSnapshotJson = JsonSerializer.Serialize(snapshot),
            TotalCalories = recipe.TotalCalories,
            TotalProtein = recipe.TotalProtein,
            TotalCarbs = recipe.TotalCarbs,
            TotalFat = recipe.TotalFat,
        };
    }

    public static RecipeMacros PerServing(decimal cal, decimal protein, decimal carbs, decimal fat, int servings)
    {
        return new RecipeMacros(
            NutritionCalculator.PerServing(cal, servings), NutritionCalculator.PerServing(protein, servings),
            NutritionCalculator.PerServing(carbs, servings), NutritionCalculator.PerServing(fat, servings));
    }
}
