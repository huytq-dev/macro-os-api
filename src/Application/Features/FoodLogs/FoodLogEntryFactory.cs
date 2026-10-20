namespace Application;

/// <summary>Một dòng cần log: món (kèm serving hoặc gram) XOR recipe (Quantity = số khẩu phần).</summary>
public sealed record EntryLine(Guid? FoodItemId, Guid? RecipeId, Guid? FoodServingId, decimal Quantity);

/// <summary>Dữ liệu tra cứu cho cả lô dòng log: món (+serving), recipe, hồ sơ chất, mã chất. Nạp bằng số truy vấn cố định, không phụ thuộc số dòng.</summary>
public sealed class EntryLookup
{
    public required Dictionary<Guid, FoodItem> Foods { get; init; }
    public required Dictionary<Guid, Dictionary<string, NutrientValue>> FoodProfiles { get; init; }
    public required Dictionary<Guid, Recipe> Recipes { get; init; }
    public required Dictionary<Guid, Dictionary<string, NutrientValue>> RecipeProfiles { get; init; }  // trên 1 khẩu phần
    public required Dictionary<string, Guid> CodeIds { get; init; }
}

/// <summary>Dựng FoodLogEntry (snapshot macro + chất) cho món hoặc recipe; dùng chung thêm đơn lẻ, batch và sửa entry.</summary>
public static class FoodLogEntryFactory
{
    public static async Task<EntryLookup> LoadAsync(IUnitOfWork uow, Guid userId, IReadOnlyCollection<EntryLine> lines, CancellationToken ct)
    {
        foreach (var l in lines)
            if (l.FoodItemId is null == l.RecipeId is null)
                throw new BadRequestException("Mỗi dòng cần đúng một trong FoodItemId hoặc RecipeId.");

        var foodIds = lines.Where(l => l.FoodItemId is not null).Select(l => l.FoodItemId!.Value).Distinct().ToList();
        var recipeIds = lines.Where(l => l.RecipeId is not null).Select(l => l.RecipeId!.Value).Distinct().ToList();

        // Món hệ thống, hoặc custom/quick-add của chính user
        var foods = foodIds.Count == 0 ? [] : await uow.Repository<FoodItem>().Query()
            .Include(f => f.Servings)
            .Where(f => foodIds.Contains(f.Id) && (f.CreatedByUserId == null || f.CreatedByUserId == userId))
            .ToDictionaryAsync(f => f.Id, ct);
        foreach (var id in foodIds)
            if (!foods.ContainsKey(id)) throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", id));

        var recipes = recipeIds.Count == 0 ? [] : await uow.Repository<Recipe>().Query()
            .Include(r => r.Ingredients)
            .Where(r => recipeIds.Contains(r.Id) && r.CreatedByUserId == userId)
            .ToDictionaryAsync(r => r.Id, ct);
        foreach (var id in recipeIds)
            if (!recipes.ContainsKey(id)) throw new NotFoundException(ExceptionMessages.NotFound("Recipe", id));

        return new EntryLookup
        {
            Foods = foods,
            FoodProfiles = foods.Count == 0 ? [] : await NutrientSnapshot.LoadProfilesAsync(uow, foods.Values.ToList(), ct),
            Recipes = recipes,
            RecipeProfiles = recipes.Count == 0 ? [] : await RecipeCalculator.PerServingProfilesAsync(uow, recipes.Values.ToList(), ct),
            CodeIds = await NutrientSnapshot.CodeIdsAsync(uow, ct),
        };
    }

    /// <summary>Serving đúng của món (404 nếu id không thuộc món); null khi log theo gram.</summary>
    public static FoodServing? ServingOf(FoodItem food, Guid? servingId)
        => servingId is { } sid
            ? food.Servings.FirstOrDefault(s => s.Id == sid) ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodServing", sid))
            : null;

    /// <summary>Tạo entry trong <paramref name="log"/> (cần Include Entries) kèm snapshot chất; chưa tính lại tổng ngày, chưa lưu.</summary>
    public static async Task<FoodLogEntry> AddAsync(IUnitOfWork uow, EntryLookup lk, EntryLine line, FoodLog log,
        TimeOnly eatenAt, MealType meal, Guid? batchId, CancellationToken ct)
    {
        var entry = new FoodLogEntry
        {
            Id = Guid.NewGuid(),
            FoodLogId = log.Id,
            MealType = meal,
            EatenAt = eatenAt,
            Quantity = line.Quantity,
            FoodLogBatchId = batchId,
        };

        if (line.RecipeId is { } rid)
        {
            if (line.FoodServingId is not null) throw new BadRequestException("Recipe không có khẩu phần riêng (FoodServingId phải để trống).");
            var recipe = lk.Recipes[rid];
            var perServing = lk.RecipeProfiles[rid];
            entry.RecipeId = rid;
            entry.RecipeVersionId = recipe.CurrentVersionId;
            entry.Source = EntrySource.Recipe;
            SnapshotColumns(entry, perServing, line.Quantity);
            await NutrientSnapshot.AddAsync(uow, entry.Id, perServing, line.Quantity, lk.CodeIds, ct);
        }
        else
        {
            var food = lk.Foods[line.FoodItemId!.Value];
            var serving = ServingOf(food, line.FoodServingId);
            var grams = FoodLogSupport.Grams(line.Quantity, serving);
            entry.FoodItemId = food.Id;
            entry.FoodServingId = serving?.Id;
            FoodLogSupport.Snapshot(entry, food, grams, lk.FoodProfiles[food.Id]);
            await NutrientSnapshot.AddAsync(uow, entry.Id, lk.FoodProfiles[food.Id], NutritionCalculator.GramFactor(grams), lk.CodeIds, ct);
        }

        await FoodLogSupport.AddEntryAsync(uow, log, entry, ct);
        return entry;
    }

    /// <summary>Macro + fiber + sodium của entry từ hồ sơ 1 khẩu phần × số khẩu phần.</summary>
    public static void SnapshotColumns(FoodLogEntry e, IReadOnlyDictionary<string, NutrientValue> perServing, decimal servings)
    {
        var m = NutritionCalculator.SnapshotFromProfile(perServing, servings);
        e.Calories = m.Calories;
        e.Protein = m.Protein;
        e.Carbs = m.Carbs;
        e.Fat = m.Fat;
        e.Fiber = m.Fiber;
        e.Sodium = m.Sodium;
    }
}
