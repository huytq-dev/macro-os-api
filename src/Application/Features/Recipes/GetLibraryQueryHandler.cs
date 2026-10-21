namespace Application;

public sealed class GetLibraryQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetLibraryQuery, QueryResult<LibraryItemModel>>
{
    // Cả hai nhánh UNION phải cùng kiểu
    private sealed class Row
    {
        public string Kind { get; set; } = "";
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Brand { get; set; }
        public FoodCategory? Category { get; set; }
        public string? IconKey { get; set; }       // chỉ món; recipe null
        public int Servings { get; set; }          // recipe: số khẩu phần; món: 0
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbs { get; set; }
        public decimal Fat { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public async Task<QueryResult<LibraryItemModel>> Handle(GetLibraryQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var top = Math.Clamp(request.Top, 1, AppConstants.MaxPageSize);
        var skip = Math.Max(request.Skip, 0);
        var search = request.Search?.Trim();

        var foodsQ = _uow.Repository<FoodItem>().Query().Where(f => f.CreatedByUserId == userId && f.Source == FoodSource.UserCreated);
        var recipesQ = _uow.Repository<Recipe>().Query().Where(r => r.CreatedByUserId == userId && !r.IsArchived);
        if (!string.IsNullOrEmpty(search))
        {
            foodsQ = foodsQ.Where(f => f.Name.Contains(search) || (f.Brand != null && f.Brand.Contains(search)));
            recipesQ = recipesQ.Where(r => r.Name.Contains(search));
        }

        var foods = foodsQ.Select(f => new Row
        {
            Kind = "Food", Id = f.Id, Name = f.Name, Brand = f.Brand, Category = f.Category, IconKey = f.IconKey, Servings = 0,
            Calories = f.CaloriesPer100g, Protein = f.ProteinPer100g, Carbs = f.CarbsPer100g, Fat = f.FatPer100g,
            UpdatedAt = f.ModifiedAt ?? f.CreatedAt,
        });
        var recipes = recipesQ.Select(r => new Row
        {
            Kind = "Recipe", Id = r.Id, Name = r.Name, Brand = null, Category = null, IconKey = r.IconKey, Servings = r.ServingsCount,
            Calories = r.TotalCalories, Protein = r.TotalProtein, Carbs = r.TotalCarbs, Fat = r.TotalFat,
            UpdatedAt = r.ModifiedAt ?? r.CreatedAt,
        });
        var q = request.Scope switch
        {
            LibraryScope.Foods => foods,
            LibraryScope.Recipes => recipes,
            _ => foods.Concat(recipes),
        };

        var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(x => x.Name).ThenBy(x => x.Kind).ThenBy(x => x.Id).Skip(skip).Take(top).ToListAsync(ct);

        // Khẩu phần mặc định của các món trong trang (một truy vấn)
        var foodIds = rows.Where(r => r.Kind == "Food").Select(r => r.Id).ToList();
        var servings = foodIds.Count == 0
            ? []
            : (await _uow.Repository<FoodServing>().Query().Where(s => foodIds.Contains(s.FoodItemId))
                .Select(s => new { s.FoodItemId, s.Id, s.ServingName, s.WeightInGrams, s.IsDefault })
                .ToListAsync(ct))
                .GroupBy(s => s.FoodItemId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.IsDefault).First());

        var overrides = await FoodIconSupport.OverridesAsync(_uow, userId, foodIds, ct);

        var items = rows.Select(r =>
        {
            if (r.Kind == "Recipe")
            {
                var m = RecipeWriter.PerServing(r.Calories, r.Protein, r.Carbs, r.Fat, r.Servings);
                return new LibraryItemModel("Recipe", r.Id, r.Name, null, null, null, "1 serving", m.Calories, m.Protein, m.Carbs, m.Fat, r.UpdatedAt, r.IconKey);
            }
            var s = servings.GetValueOrDefault(r.Id);
            var grams = s?.WeightInGrams ?? 100m;
            return new LibraryItemModel("Food", r.Id, r.Name, r.Brand, FoodEmoji.For(r.Category), s?.Id, s?.ServingName ?? "100 g",
                NutritionCalculator.Scale(r.Calories, grams), NutritionCalculator.Scale(r.Protein, grams),
                NutritionCalculator.Scale(r.Carbs, grams), NutritionCalculator.Scale(r.Fat, grams), r.UpdatedAt,
                FoodIconSupport.Effective(r.Id, r.IconKey, overrides));
        }).ToList();
        return new QueryResult<LibraryItemModel> { Items = items, TotalCount = total };
    }
}
