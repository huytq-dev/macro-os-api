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
            .Must(l => l is null || l.Count() <= 100).WithMessage("Tối đa 100 nguyên liệu.");
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
                    ?? throw new BadRequestException($"Món '{food.Name}' chưa có khẩu phần nên không dùng được làm nguyên liệu.");

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

/// <summary>WP4: tạo recipe (MealType thiếu → Lunch). Dinh dưỡng tính từ nguyên liệu; mỗi lần tạo/sửa sinh một bản RecipeVersion.</summary>
public sealed record CreateRecipeCommand(
    string Name,
    string? Description,
    string? ImageUrl,
    int ServingsCount,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    MealType? MealType,
    string? SourceUrl,
    string? Instructions,
    decimal? CookedYieldGrams,
    List<RecipeIngredientInput> Ingredients,
    List<RecipeStepInput>? Steps = null,
    string? IconKey = null
) : IRequest<Guid>;

public sealed class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeCommandValidator()
    {
        RecipeWriter.Rules(this, x => x.Name, x => x.Description, x => x.ImageUrl, x => x.ServingsCount, x => x.PrepTimeMinutes, x => x.CookTimeMinutes,
            x => x.MealType, x => x.SourceUrl, x => x.Instructions, x => x.CookedYieldGrams, x => x.Ingredients);
        RecipePreparation.Rules(this, x => x.Steps, x => x.IconKey);
    }
}

public sealed class CreateRecipeCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<CreateRecipeCommand, Guid>
{
    public async Task<Guid> Handle(CreateRecipeCommand request, CancellationToken ct)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = _userContext.UserId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            ServingsCount = request.ServingsCount,
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            MealType = request.MealType ?? MealType.Lunch,
            SourceUrl = request.SourceUrl?.Trim(),
            IconKey = request.IconKey,
            StepsJson = RecipeSteps.Serialize(request.Steps),
        };
        var version = await RecipeWriter.ApplyAsync(_uow, recipe.CreatedByUserId, recipe, 1, request.Instructions, request.CookedYieldGrams, request.Ingredients, ct);

        await _uow.Repository<Recipe>().AddAsync(recipe, ct);
        await _uow.Repository<RecipeVersion>().AddAsync(version, ct);
        await _uow.EnsureSaveAsync(ct);

        // Recipe ↔ RecipeVersion tham chiếu vòng nhau: lưu hai bên trước rồi mới trỏ CurrentVersionId
        recipe.CurrentVersionId = version.Id;
        await _uow.EnsureSaveAsync(ct);
        return recipe.Id;
    }
}

/// <summary>WP4: sửa recipe (gửi đủ object, nguyên liệu = thay toàn bộ). Sinh bản mới; entry đã log giữ snapshot cũ.</summary>
public sealed record UpdateRecipeCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    int ServingsCount,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    MealType? MealType,
    string? SourceUrl,
    string? Instructions,
    decimal? CookedYieldGrams,
    List<RecipeIngredientInput> Ingredients,
    List<RecipeStepInput>? Steps = null,
    string? IconKey = null
) : IRequest<Unit>;

public sealed class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeCommandValidator()
    {
        RecipeWriter.Rules(this, x => x.Name, x => x.Description, x => x.ImageUrl, x => x.ServingsCount, x => x.PrepTimeMinutes, x => x.CookTimeMinutes,
            x => x.MealType, x => x.SourceUrl, x => x.Instructions, x => x.CookedYieldGrams, x => x.Ingredients);
        RecipePreparation.Rules(this, x => x.Steps, x => x.IconKey);
    }
}

public sealed class UpdateRecipeCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateRecipeCommand, Unit>
{
    public async Task<Unit> Handle(UpdateRecipeCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var recipe = await _uow.Repository<Recipe>().Query(tracking: true)
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.CreatedByUserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("Recipe", request.Id));

        recipe.Name = request.Name.Trim();
        recipe.Description = request.Description?.Trim();
        recipe.ImageUrl = request.ImageUrl?.Trim();
        recipe.ServingsCount = request.ServingsCount;
        recipe.PrepTimeMinutes = request.PrepTimeMinutes;
        recipe.CookTimeMinutes = request.CookTimeMinutes;
        recipe.MealType = request.MealType ?? recipe.MealType;
        recipe.SourceUrl = request.SourceUrl?.Trim();
        recipe.IconKey = request.IconKey; // gửi đủ object: null = bỏ icon đã chọn
        recipe.StepsJson = RecipeSteps.Serialize(request.Steps); // null/rỗng = xoá hết bước

        var last = await _uow.Repository<RecipeVersion>().Query().Where(v => v.RecipeId == recipe.Id).MaxAsync(v => (int?)v.VersionNo, ct) ?? 0;
        recipe.Ingredients.Clear(); // FK bắt buộc: EF xoá các dòng bị bỏ khỏi collection
        var version = await RecipeWriter.ApplyAsync(_uow, userId, recipe, last + 1, request.Instructions, request.CookedYieldGrams, request.Ingredients, ct);

        await _uow.Repository<RecipeVersion>().AddAsync(version, ct);
        await _uow.EnsureSaveAsync(ct);
        recipe.CurrentVersionId = version.Id;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}

/// <summary>WP4: xoá (soft) recipe của mình; entry đã log giữ snapshot.</summary>
public sealed record DeleteRecipeCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteRecipeCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<DeleteRecipeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteRecipeCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var recipe = await _uow.Repository<Recipe>().FindTrackedAsync(r => r.Id == request.Id && r.CreatedByUserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("Recipe", request.Id));
        await _uow.Repository<Recipe>().RemoveAsync(recipe, ct); // soft delete qua interceptor
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}

/// <summary>WP4: chi tiết recipe của mình: nguyên liệu kèm tên món/khẩu phần, tổng và theo khẩu phần.</summary>
public sealed record GetRecipeQuery(Guid Id) : IRequest<RecipeResponse>;

public sealed class GetRecipeQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetRecipeQuery, RecipeResponse>
{
    public async Task<RecipeResponse> Handle(GetRecipeQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var r = await _uow.Repository<Recipe>().Query()
            .Where(x => x.Id == request.Id && x.CreatedByUserId == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("Recipe", request.Id));

        var version = r.CurrentVersionId is { } vid
            ? await _uow.Repository<RecipeVersion>().Query().Where(v => v.Id == vid).Select(v => new { v.Instructions, v.CookedYieldGrams }).FirstOrDefaultAsync(ct)
            : null;

        var ingredients = await _uow.Repository<RecipeIngredient>().Query()
            .Where(i => i.RecipeId == r.Id).OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .ToListAsync(ct);
        // Món nguyên liệu đã xoá mềm vẫn hiện (recipe giữ nguyên dinh dưỡng)
        var foodIds = ingredients.Select(i => i.FoodItemId).Distinct().ToList();
        var foods = await _uow.Repository<FoodItem>().Query().IgnoreQueryFilters()
            .Where(f => foodIds.Contains(f.Id))
            .Select(f => new { f.Id, f.Name, f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g })
            .ToDictionaryAsync(f => f.Id, ct);
        var servingIds = ingredients.Select(i => i.FoodServingId).Distinct().ToList();
        var servings = await _uow.Repository<FoodServing>().Query()
            .Where(s => servingIds.Contains(s.Id)).Select(s => new { s.Id, s.ServingName })
            .ToDictionaryAsync(s => s.Id, s => s.ServingName, ct);

        var models = ingredients.Select(i =>
        {
            var f = foods[i.FoodItemId];
            var grams = i.GramsNormalized ?? 0m;
            return new RecipeIngredientModel(i.Id, i.FoodItemId, f.Name, i.FoodServingId, servings.GetValueOrDefault(i.FoodServingId, "100 g"),
                i.Quantity, grams, NutritionCalculator.Scale(f.CaloriesPer100g, grams), NutritionCalculator.Scale(f.ProteinPer100g, grams),
                NutritionCalculator.Scale(f.CarbsPer100g, grams), NutritionCalculator.Scale(f.FatPer100g, grams), i.Notes);
        }).ToList();

        return new RecipeResponse(
            r.Id, r.Name, r.Description, r.ImageUrl, r.ServingsCount, r.PrepTimeMinutes, r.CookTimeMinutes, r.MealType.ToString(), r.SourceUrl,
            version?.Instructions, version?.CookedYieldGrams, models.Sum(m => m.Grams),
            new RecipeMacros(r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat),
            RecipeWriter.PerServing(r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat, r.ServingsCount),
            models, RecipeSteps.Parse(r.StepsJson), r.IconKey);
    }
}

/// <summary>WP4: danh sách recipe của mình (theo tên), có tìm kiếm và phân trang.</summary>
public sealed record GetRecipesQuery(string? Search, int Top = AppConstants.DefaultPageTop, int Skip = AppConstants.DefaultPageSkip)
    : IRequest<QueryResult<RecipeSummaryModel>>;

public sealed class GetRecipesQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetRecipesQuery, QueryResult<RecipeSummaryModel>>
{
    public async Task<QueryResult<RecipeSummaryModel>> Handle(GetRecipesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var top = Math.Clamp(request.Top, 1, AppConstants.MaxPageSize);
        var skip = Math.Max(request.Skip, 0);

        var q = _uow.Repository<Recipe>().Query().Where(r => r.CreatedByUserId == userId && !r.IsArchived);
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search)) q = q.Where(r => r.Name.Contains(search));

        var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(r => r.Name).ThenBy(r => r.Id).Skip(skip).Take(top)
            .Select(r => new
            {
                r.Id, r.Name, r.ImageUrl, r.ServingsCount, r.MealType,
                r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat, r.CreatedAt, r.ModifiedAt, r.IconKey,
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new RecipeSummaryModel(
            r.Id, r.Name, r.ImageUrl, r.ServingsCount, r.MealType.ToString(),
            RecipeWriter.PerServing(r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat, r.ServingsCount),
            r.ModifiedAt ?? r.CreatedAt, r.IconKey)).ToList();
        return new QueryResult<RecipeSummaryModel> { Items = items, TotalCount = total };
    }
}
