namespace Application;

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

        // Món hệ thống (catalog/USDA: gần như bất biến) lấy từ cache nếu có; món custom/quick-add của chính user luôn đọc DB. Cache chỉ đọc, không gắn vào DbContext.
        var foods = new Dictionary<Guid, FoodItem>();
        var profiles = new Dictionary<Guid, Dictionary<string, NutrientValue>>();
        var missing = new List<Guid>();
        foreach (var id in foodIds)
        {
            if (uow.Cache.TryGet<(FoodItem Food, Dictionary<string, NutrientValue> Profile)>($"food:{id}", out var hit))
            {
                foods[id] = hit.Food;
                profiles[id] = new Dictionary<string, NutrientValue>(hit.Profile);
            }
            else missing.Add(id);
        }
        if (missing.Count > 0)
        {
            var loaded = await uow.Repository<FoodItem>().Query()
                .Include(f => f.Servings)
                .Where(f => missing.Contains(f.Id) && (f.CreatedByUserId == null || f.CreatedByUserId == userId))
                .ToDictionaryAsync(f => f.Id, ct);
            foreach (var id in missing)
                if (!loaded.ContainsKey(id)) throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", id));

            var loadedProfiles = await NutrientSnapshot.LoadProfilesAsync(uow, loaded.Values.ToList(), ct);
            foreach (var (id, food) in loaded)
            {
                foods[id] = food;
                profiles[id] = loadedProfiles[id];
                if (food.CreatedByUserId is null) uow.Cache.Set($"food:{id}", (food, new Dictionary<string, NutrientValue>(loadedProfiles[id])), TimeSpan.FromMinutes(5));
            }
        }

        var recipes = recipeIds.Count == 0 ? [] : await uow.Repository<Recipe>().Query()
            .Include(r => r.Ingredients)
            .Where(r => recipeIds.Contains(r.Id) && r.CreatedByUserId == userId)
            .ToDictionaryAsync(r => r.Id, ct);
        foreach (var id in recipeIds)
            if (!recipes.ContainsKey(id)) throw new NotFoundException(ExceptionMessages.NotFound("Recipe", id));

        return new EntryLookup
        {
            Foods = foods,
            FoodProfiles = profiles,
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
