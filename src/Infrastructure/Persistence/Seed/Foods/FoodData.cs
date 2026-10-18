namespace Infrastructure;

/// <summary>
/// Danh mục thực phẩm phổ biến (số liệu ước lượng /100 g theo bảng thành phần thực phẩm và USDA).
/// Chạy mọi môi trường và idempotent theo (ProviderCode, ExternalFoodId): chỉ thêm món chưa có.
/// </summary>
public static class FoodData
{
    private const string Provider = "seed";

    // Id, tên, nhóm, kcal, P, C, F (/100 g), tên khẩu phần mặc định, gram khẩu phần mặc định
    private static readonly (string Id, string Name, FoodCategory Category, decimal Kcal, decimal P, decimal C, decimal F, string Serving, decimal Grams)[] Foods =
    [
        ("egg-boiled", "Trứng gà luộc", FoodCategory.Protein, 155, 12.6m, 1.1m, 10.6m, "1 quả", 50),
        ("chicken-breast", "Ức gà nấu chín", FoodCategory.Protein, 165, 31, 0, 3.6m, "1 miếng", 120),
        ("chicken-thigh", "Đùi gà nấu chín (bỏ da)", FoodCategory.Protein, 209, 26, 0, 10.9m, "1 cái", 100),
        ("beef-lean", "Thịt bò nạc nấu chín", FoodCategory.Protein, 250, 26, 0, 15, "1 phần", 100),
        ("pork-lean", "Thịt heo nạc nấu chín", FoodCategory.Protein, 242, 27, 0, 14, "1 phần", 100),
        ("salmon-grilled", "Cá hồi nướng", FoodCategory.Protein, 206, 22, 0, 12, "1 miếng", 150),
        ("shrimp-boiled", "Tôm luộc", FoodCategory.Protein, 99, 24, 0.2m, 0.3m, "1 phần", 85),
        ("tofu", "Đậu hũ", FoodCategory.Protein, 76, 8, 1.9m, 4.8m, "1 miếng", 80),
        ("tuna-canned", "Cá ngừ hộp", FoodCategory.Protein, 116, 26, 0, 1, "1 hộp", 100),
        ("milk-fresh", "Sữa tươi không đường", FoodCategory.Dairy, 61, 3.2m, 4.8m, 3.3m, "1 ly", 200),
        ("yogurt-plain", "Sữa chua không đường", FoodCategory.Dairy, 61, 3.5m, 4.7m, 3.3m, "1 hộp", 100),
        ("yogurt-sweet", "Sữa chua có đường", FoodCategory.Dairy, 90, 3.5m, 14, 2, "1 hộp", 100),
        ("cheddar", "Phô mai cheddar", FoodCategory.Dairy, 403, 25, 1.3m, 33, "1 lát", 28),
        ("whey", "Whey protein", FoodCategory.Supplements, 400, 80, 8, 6, "1 muỗng", 30),
        ("rice-white", "Cơm trắng", FoodCategory.Grains, 130, 2.7m, 28, 0.3m, "1 chén", 150),
        ("rice-brown", "Cơm gạo lứt", FoodCategory.Grains, 112, 2.3m, 23.5m, 0.8m, "1 chén", 150),
        ("sticky-rice", "Xôi", FoodCategory.Grains, 97, 2, 21, 0.2m, "1 gói", 150),
        ("bun", "Bún tươi", FoodCategory.Grains, 110, 1.7m, 25, 0.1m, "1 tô", 200),
        ("pho-noodle", "Bánh phở", FoodCategory.Grains, 109, 0.9m, 25, 0.2m, "1 tô", 200),
        ("bread-roll", "Bánh mì không", FoodCategory.Grains, 265, 9, 49, 3.2m, "1 ổ", 80),
        ("bread-slice", "Bánh mì sandwich", FoodCategory.Grains, 266, 8, 50, 3.3m, "1 lát", 28),
        ("oats", "Yến mạch khô", FoodCategory.Grains, 389, 16.9m, 66, 6.9m, "1/2 chén", 40),
        ("sweet-potato", "Khoai lang luộc", FoodCategory.Grains, 86, 1.6m, 20, 0.1m, "1 củ", 130),
        ("potato", "Khoai tây luộc", FoodCategory.Grains, 87, 1.9m, 20, 0.1m, "1 củ", 150),
        ("corn", "Bắp luộc", FoodCategory.Grains, 96, 3.4m, 21, 1.5m, "1 bắp", 140),
        ("tomato", "Cà chua", FoodCategory.Vegetables, 18, 0.9m, 3.9m, 0.2m, "1 quả", 123),
        ("carrot", "Cà rốt", FoodCategory.Vegetables, 41, 0.9m, 9.6m, 0.2m, "1 củ", 60),
        ("cucumber", "Dưa leo", FoodCategory.Vegetables, 15, 0.7m, 3.6m, 0.1m, "1 quả", 150),
        ("broccoli", "Bông cải xanh luộc", FoodCategory.Vegetables, 35, 2.4m, 7.2m, 0.4m, "1 chén", 90),
        ("spinach", "Cải bó xôi", FoodCategory.Vegetables, 23, 2.9m, 3.6m, 0.4m, "1 chén", 30),
        ("cabbage", "Bắp cải", FoodCategory.Vegetables, 25, 1.3m, 5.8m, 0.1m, "1 chén", 90),
        ("water-spinach", "Rau muống", FoodCategory.Vegetables, 19, 2.6m, 3.1m, 0.2m, "1 phần", 100),
        ("banana", "Chuối", FoodCategory.Fruits, 89, 1.1m, 22.8m, 0.3m, "1 quả", 118),
        ("apple", "Táo", FoodCategory.Fruits, 52, 0.3m, 13.8m, 0.2m, "1 quả", 182),
        ("orange", "Cam", FoodCategory.Fruits, 47, 0.9m, 11.8m, 0.1m, "1 quả", 130),
        ("mango", "Xoài", FoodCategory.Fruits, 60, 0.8m, 15, 0.4m, "1 chén", 165),
        ("watermelon", "Dưa hấu", FoodCategory.Fruits, 30, 0.6m, 7.6m, 0.2m, "1 chén", 150),
        ("avocado", "Bơ", FoodCategory.Fruits, 160, 2, 8.5m, 14.7m, "1/2 quả", 100),
        ("dragon-fruit", "Thanh long", FoodCategory.Fruits, 60, 1.2m, 13, 0.4m, "1/2 quả", 150),
        ("papaya", "Đu đủ", FoodCategory.Fruits, 43, 0.5m, 11, 0.3m, "1 chén", 140),
        ("pineapple", "Dứa", FoodCategory.Fruits, 50, 0.5m, 13, 0.1m, "1 chén", 165),
        ("olive-oil", "Dầu ăn", FoodCategory.Fats, 884, 0, 0, 100, "1 muỗng canh", 13.5m),
        ("peanut-butter", "Bơ đậu phộng", FoodCategory.Fats, 588, 25, 20, 50, "1 muỗng canh", 16),
        ("almond", "Hạnh nhân", FoodCategory.Fats, 579, 21, 22, 50, "1 nắm", 28),
        ("cashew", "Hạt điều", FoodCategory.Fats, 553, 18, 30, 44, "1 nắm", 28),
        ("coffee-black", "Cà phê đen", FoodCategory.Beverages, 2, 0.1m, 0, 0, "1 ly", 240),
        ("coffee-milk", "Cà phê sữa đá", FoodCategory.Beverages, 70, 1.5m, 12, 1.8m, "1 ly", 250),
        ("bubble-tea", "Trà sữa trân châu", FoodCategory.Beverages, 70, 1, 14, 1.5m, "1 ly", 500),
        ("orange-juice", "Nước cam ép", FoodCategory.Beverages, 45, 0.7m, 10.4m, 0.2m, "1 ly", 250),
        ("cola", "Nước ngọt có gas", FoodCategory.Beverages, 42, 0, 10.6m, 0, "1 lon", 330),
        ("dark-chocolate", "Socola đen", FoodCategory.Snacks, 598, 7.8m, 46, 43, "1 thanh", 20),
        ("potato-chips", "Snack khoai tây", FoodCategory.Snacks, 536, 7, 53, 35, "1 gói nhỏ", 30),
        ("pho-beef", "Phở bò", FoodCategory.FastFood, 65, 4.5m, 8, 1.4m, "1 tô", 550),
        ("bun-bo", "Bún bò Huế", FoodCategory.FastFood, 70, 4, 9, 2, "1 tô", 600),
        ("banh-mi-thit", "Bánh mì thịt", FoodCategory.FastFood, 250, 10, 30, 9, "1 ổ", 180),
        ("com-tam", "Cơm tấm sườn", FoodCategory.FastFood, 190, 8, 24, 6, "1 dĩa", 400),
        ("goi-cuon", "Gỏi cuốn", FoodCategory.FastFood, 115, 5.5m, 17, 2.5m, "1 cuốn", 60),
        ("pizza", "Pizza", FoodCategory.FastFood, 266, 11, 33, 10, "1 lát", 107),
        ("hamburger", "Hamburger", FoodCategory.FastFood, 260, 13, 28, 11, "1 cái", 200),
        ("french-fries", "Khoai tây chiên", FoodCategory.FastFood, 312, 3.4m, 41, 15, "1 phần vừa", 117),
    ];

    // Alias en-US (tìm theo tên tiếng Anh, vd bước resolve nguyên liệu của AI Plate). Key = ExternalFoodId của món seed; nhiều alias cách nhau bằng '|'.
    private static readonly Dictionary<string, string> EnglishAliases = new()
    {
        ["egg-boiled"] = "Egg|Boiled egg", ["chicken-breast"] = "Chicken breast|Chicken", ["chicken-thigh"] = "Chicken thigh",
        ["beef-lean"] = "Beef|Lean beef|Steak", ["pork-lean"] = "Pork|Lean pork", ["salmon-grilled"] = "Salmon|Grilled salmon",
        ["shrimp-boiled"] = "Shrimp|Prawn", ["tofu"] = "Tofu", ["tuna-canned"] = "Tuna|Canned tuna", ["milk-fresh"] = "Milk|Fresh milk",
        ["yogurt-plain"] = "Yogurt|Plain yogurt", ["yogurt-sweet"] = "Sweetened yogurt", ["cheddar"] = "Cheese|Cheddar",
        ["whey"] = "Whey|Whey protein|Protein powder", ["rice-white"] = "White rice|Rice|Cooked rice", ["rice-brown"] = "Brown rice",
        ["sticky-rice"] = "Sticky rice|Glutinous rice", ["bun"] = "Rice vermicelli|Vermicelli", ["pho-noodle"] = "Pho noodles|Rice noodles",
        ["bread-roll"] = "Bread roll|Baguette|Bread", ["bread-slice"] = "Sandwich bread|Bread slice|Toast", ["oats"] = "Oats|Oatmeal|Rolled oats",
        ["sweet-potato"] = "Sweet potato", ["potato"] = "Potato|Boiled potato", ["corn"] = "Corn|Sweetcorn",
        ["tomato"] = "Tomato", ["carrot"] = "Carrot", ["cucumber"] = "Cucumber", ["broccoli"] = "Broccoli", ["spinach"] = "Spinach",
        ["cabbage"] = "Cabbage", ["water-spinach"] = "Water spinach|Morning glory", ["banana"] = "Banana", ["apple"] = "Apple",
        ["orange"] = "Orange", ["mango"] = "Mango", ["watermelon"] = "Watermelon", ["avocado"] = "Avocado", ["dragon-fruit"] = "Dragon fruit|Pitaya",
        ["papaya"] = "Papaya", ["pineapple"] = "Pineapple", ["olive-oil"] = "Oil|Cooking oil|Olive oil", ["peanut-butter"] = "Peanut butter",
        ["almond"] = "Almond|Almonds", ["cashew"] = "Cashew|Cashews", ["coffee-black"] = "Black coffee|Coffee", ["coffee-milk"] = "Iced milk coffee|Milk coffee",
        ["bubble-tea"] = "Bubble tea|Boba|Milk tea", ["orange-juice"] = "Orange juice", ["cola"] = "Cola|Soda|Soft drink",
        ["dark-chocolate"] = "Dark chocolate|Chocolate", ["potato-chips"] = "Potato chips|Chips|Crisps", ["pho-beef"] = "Beef pho|Pho",
        ["bun-bo"] = "Hue beef noodle soup|Bun bo Hue", ["banh-mi-thit"] = "Banh mi|Vietnamese sandwich", ["com-tam"] = "Broken rice with pork chop|Com tam",
        ["goi-cuon"] = "Fresh spring roll|Summer roll", ["pizza"] = "Pizza", ["hamburger"] = "Hamburger|Burger", ["french-fries"] = "French fries|Fries",
    };

    /// <summary>Alias en-US của món seed theo ExternalFoodId (dùng lại cho thực phẩm chuẩn).</summary>
    public static IReadOnlyDictionary<string, string> EnglishAliasMap => EnglishAliases;

    public const string SeedProvider = Provider;

    private static string Normalize(string s) => FoodNameNormalizer.Normalize(s);

    private static async Task SeedAliasesAsync(ApplicationDbContext context, CancellationToken ct)
    {
        var foods = await context.Set<FoodItem>().IgnoreQueryFilters()
            .Where(f => f.ProviderCode == Provider).Select(f => new { f.Id, f.ExternalFoodId }).ToListAsync(ct);
        var have = (await context.Set<FoodAlias>().Where(a => a.Locale == "en-US").Select(a => new { a.FoodItemId, a.AliasNormalized }).ToListAsync(ct))
            .Select(a => (a.FoodItemId, a.AliasNormalized)).ToHashSet();

        var added = false;
        foreach (var f in foods.Where(f => f.ExternalFoodId is not null && EnglishAliases.ContainsKey(f.ExternalFoodId!)))
            foreach (var alias in EnglishAliases[f.ExternalFoodId!].Split('|'))
            {
                var norm = Normalize(alias);
                if (!have.Add((f.Id, norm))) continue;
                context.Set<FoodAlias>().Add(new FoodAlias { Id = Guid.NewGuid(), FoodItemId = f.Id, Locale = "en-US", Alias = alias, AliasNormalized = norm });
                added = true;
            }
        if (added) await context.SaveChangesAsync(ct);
    }

    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken ct = default)
    {
        var existing = await context.Set<FoodItem>()
            .IgnoreQueryFilters()
            .Where(f => f.ProviderCode == Provider)
            .Select(f => f.ExternalFoodId!)
            .ToListAsync(ct);

        var missing = Foods.Where(f => !existing.Contains(f.Id)).ToList();
        if (missing.Count == 0) { await SeedAliasesAsync(context, ct); return; }

        foreach (var f in missing)
        {
            context.Set<FoodItem>().Add(new FoodItem
            {
                Id = Guid.NewGuid(),
                Name = f.Name,
                Category = f.Category,
                Source = FoodSource.System,
                IsVerified = true,
                QualityStatus = FoodQualityStatus.Verified,
                ProviderCode = Provider,
                ExternalFoodId = f.Id,
                Country = "VN",
                Language = "vi-VN",
                IconKey = IconKeyResolver.Resolve(f.Name, f.Category),
                CaloriesPer100g = f.Kcal,
                ProteinPer100g = f.P,
                CarbsPer100g = f.C,
                FatPer100g = f.F,
                Servings =
                [
                    new FoodServing { Id = Guid.NewGuid(), ServingName = f.Serving, WeightInGrams = f.Grams, UnitType = ServingUnitType.Household, IsDefault = true },
                    new FoodServing { Id = Guid.NewGuid(), ServingName = "100 g", WeightInGrams = 100, UnitType = ServingUnitType.Mass, IsDefault = false },
                ],
            });
        }
        await context.SaveChangesAsync(ct);
        await SeedAliasesAsync(context, ct);
    }
}
