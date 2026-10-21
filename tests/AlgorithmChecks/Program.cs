using Contract;
// Self-check cho MacroTargetCalculator (ALG-01). Chạy: dotnet run --project tests/AlgorithmChecks
using Application;
using Domain;
using Infrastructure;

static void Check(bool ok, string name) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}"); if (!ok) Environment.ExitCode = 1; }

// Nam 30 tuổi, 175 cm, 80 kg, hoạt động vừa: BMR = 800+1093.75-150+5 = 1748.75 → ×1.55 = 2710.56 → 2711
var tdee = MacroTargetCalculator.InitialTdee(Gender.Male, 30, 175m, 80m, ActivityLevel.ModeratelyActive);
Check(tdee == 2711, $"InitialTdee = {tdee}");

Check(MacroTargetCalculator.AgeYears(new DateOnly(1996, 10, 16), new DateOnly(2026, 10, 15)) == 29, "AgeYears chưa tới sinh nhật");
Check(MacroTargetCalculator.AgeYears(new DateOnly(1996, 10, 15), new DateOnly(2026, 10, 15)) == 30, "AgeYears đúng sinh nhật");

// Giảm 0.5 %/tuần ở 80 kg = 0.4 kg/tuần → deficit 7700*0.4/7 = 440 → 2271
Check(MacroTargetCalculator.DailyCalories(2711, 0.5m, 80m, 7700, null) == 2271, "DailyCalories giảm 0.5 %");
// Tăng cân: rate âm → surplus
Check(MacroTargetCalculator.DailyCalories(2711, -0.25m, 80m, 7700, null) == 2931, "DailyCalories tăng 0.25 %");
// Floor chặn dưới
Check(MacroTargetCalculator.DailyCalories(1500, 1.5m, 80m, 7700, 1200) == 1200, "Calorie floor");

Check(MacroTargetCalculator.ProteinGPerKg(ProteinLevel.Moderate, TrainingType.Lifting) == 2.0m, "Protein Moderate + Lifting = 2.0");

// Balanced 2271 kcal, 80 kg, 1.8 g/kg: P=144, F=max(2271*0.3/9=75.7, 50.5)=75.7, C=(2271-576-681.3)/4=253.4
var balanced = MacroTargetCalculator.SplitMacros(2271, 80m, 1.8m, null, DietStyle.Balanced);
Check(balanced.ProteinG == 144m && balanced.FatG == 75.7m && balanced.CarbsG == 253.4m, $"Balanced split {balanced}");

var keto = MacroTargetCalculator.SplitMacros(2271, 80m, 1.8m, null, DietStyle.Keto);
Check(keto.CarbsG == 30m && keto.FatG > 150m, $"Keto split {keto}");

var lowCarb = MacroTargetCalculator.SplitMacros(2271, 80m, 1.8m, null, DietStyle.LowCarb);
Check(lowCarb.CarbsG <= 200m && lowCarb.CarbsG <= 2271m * 0.30m / 4m, $"LowCarb cap {lowCarb}");

// Macro cộng lại ≈ kcal
Check(Math.Abs(4 * balanced.ProteinG + 4 * balanced.CarbsG + 9 * balanced.FatG - 2271) < 2, "Balanced kcal khớp");


// ALG-02: EMA N=20 trên chuỗi có ngày thiếu
var d0 = new DateOnly(2026, 10, 1);
var trends = WeightTrendCalculator.Compute([(d0, 80m), (d0.AddDays(2), 79m), (d0.AddDays(3), 81m)], 20);
Check(trends[d0] == 80m, "Trend ngày đầu = cân thô");
// a=2/21; ngày1 nội suy 79.5 → 80 + a*(79.5-80) = 79.952; ngày2 79 → 79.952 + a*(79-79.952) = 79.861 → 79.86
Check(trends[d0.AddDays(2)] == 79.86m, $"Trend sau nội suy = {trends[d0.AddDays(2)]}");
Check(trends[d0.AddDays(3)] < 80.0m && trends[d0.AddDays(3)] > 79.86m, "Trend ít nhạy với nhiễu 81 kg");
Check(trends.Count == 3, "Chỉ trả trend cho ngày có cân");
Check(WeightTrendCalculator.RatePerWeek(d0, 80m, d0.AddDays(14), 79m) == -0.5m, "RatePerWeek -0.5 kg/tuần");

// MOS-005: snapshot entry + tổng ngày
var rice = new FoodItem { Id = Guid.NewGuid(), Name = "Rice", CaloriesPer100g = 130, ProteinPer100g = 2.7m, CarbsPer100g = 28, FatPer100g = 0.3m };
var riceProfile = new Dictionary<string, NutrientValue> { ["fiber_g"] = new(0.4m, NutrientAmountStatus.Measured) };
var bowl = new FoodServing { Id = Guid.NewGuid(), FoodItemId = rice.Id, ServingName = "bowl", WeightInGrams = 150 };
var e1 = new FoodLogEntry(); FoodLogSupport.Snapshot(e1, rice, FoodLogSupport.Grams(2, bowl), riceProfile);   // 300 g
var e2 = new FoodLogEntry(); FoodLogSupport.Snapshot(e2, rice, FoodLogSupport.Grams(50, null), riceProfile);  // 50 g
Check(e1.Calories == 390m && e1.Protein == 8.1m && e1.Fiber == 1.2m, $"Snapshot 2 bowl = {e1.Calories} kcal");
Check(e2.Calories == 65m && e2.Carbs == 14m, "Snapshot 50 g không serving");
var log = new FoodLog { Entries = [e1, e2, new FoodLogEntry { Calories = 999, IsDeleted = true }] };
FoodLogSupport.RecalcTotals(log);
Check(log.TotalCalories == 455m && log.TotalFiber == 1.4m, $"Tổng ngày bỏ entry đã xoá = {log.TotalCalories}");

// WP1: USDA parser + mapper (JSON mẫu rút gọn: 208 kcal, 203/204/205 macro, 291 fiber, 313 fluoride µg, 268 kJ bỏ, amount thiếu bỏ)
using var usdaDoc = System.Text.Json.JsonDocument.Parse("""
[{"fdcId":171688,"description":"Apples, raw, with skin","dataType":"SR Legacy","foodCategory":{"description":"Fruits and Fruit Juices"},
 "foodNutrients":[
  {"nutrient":{"number":"208","unitName":"kcal"},"amount":52},{"nutrient":{"number":"268","unitName":"kJ"},"amount":218},
  {"nutrient":{"number":"203","unitName":"g"},"amount":0.26},{"nutrient":{"number":"204","unitName":"g"},"amount":0.17},{"nutrient":{"number":"205","unitName":"g"},"amount":13.81},
  {"nutrient":{"number":"291","unitName":"g"},"amount":2.4},{"nutrient":{"number":"313","unitName":"µg"},"amount":3.3},{"nutrient":{"number":"301","unitName":"mg"}}],
 "foodPortions":[{"gramWeight":109,"amount":1,"modifier":"cup, sliced","measureUnit":{"name":"undetermined"}},{"gramWeight":182,"portionDescription":"1 medium"}]},
 {"fdcId":1,"description":"No energy","dataType":"Foundation","foodNutrients":[{"nutrient":{"number":"203","unitName":"g"},"amount":1}]}]
""");
var usdaFoods = UsdaFoodParser.ParseMany(usdaDoc.RootElement);
Check(usdaFoods.Count == 2 && usdaFoods[0].Nutrients.Count == 7 && usdaFoods[0].Portions.Count == 2 && usdaFoods[0].Portions[0].Name == "1 cup, sliced", "USDA parser: bỏ amount thiếu, dựng tên khẩu phần");
var defs = new Dictionary<string, UsdaFoodMapper.Definition>
{
    ["208"] = new(Guid.NewGuid(), "energy_kcal", "kcal"), ["203"] = new(Guid.NewGuid(), "protein_g", "g"), ["204"] = new(Guid.NewGuid(), "fat_g", "g"),
    ["205"] = new(Guid.NewGuid(), "carbs_g", "g"), ["291"] = new(Guid.NewGuid(), "fiber_g", "g"), ["313"] = new(Guid.NewGuid(), "fluoride_mcg", "mcg"),
};
var apple = UsdaFoodMapper.Map(usdaFoods[0], defs, DateTimeOffset.UtcNow);
Check(apple is not null && apple.Food.CaloriesPer100g == 52m && apple.Food.CarbsPer100g == 13.81m && apple.Food.ExternalFoodId == "171688" && apple.Food.Source == FoodSource.USDA, "USDA mapper: macro vào cột FoodItem");
Check(apple!.Nutrients.Select(n => n.Code).Order().SequenceEqual(["fiber_g", "fluoride_mcg"]), "USDA mapper: chất khác thành FoodNutrient, µg khớp mcg, macro không lặp");
Check(apple.Food.Servings.Count == 3 && apple.Food.Servings.Single(s => s.IsDefault).ServingName == "1 cup, sliced" && apple.Food.Servings.Any(s => s.ServingName == "100 g"), "USDA mapper: khẩu phần theo portion + 100 g");
Check(UsdaFoodMapper.Map(usdaFoods[1], defs, DateTimeOffset.UtcNow) is null, "USDA mapper: thiếu năng lượng → bỏ qua món");

// WP4: recipe = tổng nguyên liệu / số khẩu phần; chất thiếu ở một nguyên liệu → Estimated
IReadOnlyDictionary<string, NutrientValue> oats = new Dictionary<string, NutrientValue> { ["energy_kcal"] = new(400, NutrientAmountStatus.Measured), ["fiber_g"] = new(10, NutrientAmountStatus.Measured) };
IReadOnlyDictionary<string, NutrientValue> milk = new Dictionary<string, NutrientValue> { ["energy_kcal"] = new(60, NutrientAmountStatus.Measured) };
var recipeTotal = NutritionCalculator.SumProfiles([(oats, 50m), (milk, 200m)]);
Check(recipeTotal["energy_kcal"].Amount == 320m && recipeTotal["energy_kcal"].Status == NutrientAmountStatus.Measured && recipeTotal["fiber_g"].Amount == 5m && recipeTotal["fiber_g"].Status == NutrientAmountStatus.Estimated, "Recipe: tổng nguyên liệu, chất thiếu → Estimated");
Check(NutritionCalculator.PerServing(recipeTotal, 2)["energy_kcal"].Amount == 160m, "Recipe: chia số khẩu phần");
var recipeEntry = new FoodLogEntry();
FoodLogEntryFactory.SnapshotColumns(recipeEntry, NutritionCalculator.PerServing(recipeTotal, 2), 1.5m);
Check(recipeEntry.Calories == 240m && recipeEntry.Fiber == 3.75m && recipeEntry.Sodium is null, "Recipe: snapshot entry = 1 khẩu phần × số khẩu phần");

// WP3: batch/bulk validator + giờ của lô
var batchValidator = new AddEntriesBatchCommandValidator();
var okItem = new BatchEntryItem(Guid.NewGuid(), null, null, 1);
Check(batchValidator.Validate(new AddEntriesBatchCommand("k1", null, null, null, "plate", [okItem])).IsValid, "Batch: lô hợp lệ");
Check(!batchValidator.Validate(new AddEntriesBatchCommand("", null, null, null, null, [okItem])).IsValid && !batchValidator.Validate(new AddEntriesBatchCommand("k", null, null, null, null, [])).IsValid, "Batch: thiếu key / rỗng bị từ chối");
Check(!batchValidator.Validate(new AddEntriesBatchCommand("k", null, null, null, null, [okItem with { RecipeId = Guid.NewGuid() }])).IsValid && !batchValidator.Validate(new AddEntriesBatchCommand("k", null, null, null, null, [okItem with { FoodItemId = null }])).IsValid, "Batch: dòng phải có đúng một của món/recipe");
Check(!new BulkMoveEntriesCommandValidator().Validate(new BulkMoveEntriesCommand([Guid.NewGuid()], null, null, null)).IsValid && new BulkMoveEntriesCommandValidator().Validate(new BulkMoveEntriesCommand([Guid.NewGuid()], new DateOnly(2026, 10, 1), null, null)).IsValid, "Bulk move: cần ít nhất một đích");
Check(!new BulkDeleteEntriesCommandValidator().Validate(new BulkDeleteEntriesCommand([])).IsValid && !new BulkDeleteEntriesCommandValidator().Validate(new BulkDeleteEntriesCommand(Enumerable.Range(0, 201).Select(_ => Guid.NewGuid()).ToList())).IsValid, "Bulk delete: rỗng / quá 200 bị từ chối");
var today = new DateOnly(2026, 10, 18);
Check(FoodLogSupport.ResolveTime(today, null, null, (today, new TimeOnly(8, 30))).EatenAt == new TimeOnly(8, 30) && FoodLogSupport.ResolveTime(today.AddDays(-1), null, MealType.Dinner, (today, new TimeOnly(8, 30))).EatenAt == MealTimes.Default(MealType.Dinner), "ResolveTime: hôm nay → giờ hiện tại, ngày khác → giờ mặc định bữa");

// WP2: nước + custom food nutrients
Check(!new AddWaterCommandValidator().Validate(new AddWaterCommand(null, 0, null, null)).IsValid && new AddWaterCommandValidator().Validate(new AddWaterCommand(null, 250, null, "Tea")).IsValid, "Water: ml phải trong 1–5000");
var nutAmounts = FoodNutrientStore.Amounts(new FoodNutrition(100, 1, 1, 1, FiberPer100g: 2, SodiumPer100g: 5), [new FoodNutrientInput("sodium_mg", 9), new FoodNutrientInput("zinc_mg", 1.5m)]);
Check(nutAmounts["fiber_g"] == 2 && nutAmounts["sodium_mg"] == 9 && nutAmounts["zinc_mg"] == 1.5m && nutAmounts.Count == 3, "Custom food: 11 trường cũ + nutrients[] (nutrients[] thắng)");
Check(!new FoodNutrientInputValidator().Validate(new FoodNutrientInput("energy_kcal", 1)).IsValid && new FoodNutrientInputValidator().Validate(new FoodNutrientInput("zinc_mg", 1)).IsValid, "Custom food: macro không nhập qua nutrients[]");
var contract = FoodNutrientStore.ToContract(new FoodItem { CaloriesPer100g = 100 }, new Dictionary<string, NutrientValue> { ["iron_mg"] = new(2.5m, NutrientAmountStatus.Measured) });
Check(contract.IronPer100g == 2.5m && contract.FiberPer100g is null, "Custom food: ToContract đọc từ hồ sơ chất");

// Canonical Food Catalog: chuẩn hoá tên, xếp hạng, dữ liệu seed, cache key
Check(FoodNameNormalizer.Normalize("Nước mắm") == "nuoc mam" && FoodNameNormalizer.Normalize("  Đường  cát!") == "duong cat" && FoodNameNormalizer.Normalize("Beef, ground, raw") == "beef ground raw", "Normalizer: bỏ dấu tiếng Việt (kể cả đ) và dấu câu");
Check(FoodNameNormalizer.Singular("eggs") == "egg" && FoodNameNormalizer.Singular("green onions") == "green onion" && FoodNameNormalizer.Singular("glass") == "glass", "Normalizer: số ít");
Check(FoodNameNormalizer.DetectLocale("Bò xào trứng") == "vi-VN" && FoodNameNormalizer.DetectLocale("Beef") == "en-US" && FoodNameNormalizer.DetectLocale("bo xao trung") == "en-US", "Normalizer: locale theo dấu");
var cands = new[] { "Beef, bone", "Beef, ground, raw", "Beef" };
var noAlias = (string _) => (IEnumerable<string>)[];
Check(FoodMatchRanker.Rank(cands, "beef", c => c, noAlias, _ => 0).First() == "Beef", "Matching: 'Beef' khớp hẳn thắng");
Check(FoodMatchRanker.Rank(cands.Take(2), "beef", c => c, noAlias, _ => 0).First() == "Beef, ground, raw", "Matching: 'Beef' không ra 'Beef, bone' khi chỉ có biến thể");
var viFoods = new[] { ("Nước mắm", new[] { "nuoc mam", "fish sauce" }), ("Nước mắm cá cơm đóng chai", new string[0]), ("Nước tương", new[] { "xi dau" }) };
Check(FoodMatchRanker.Rank(viFoods, FoodNameNormalizer.Normalize("nuoc mam"), f => f.Item1, f => f.Item2, _ => 0).First().Item1 == "Nước mắm", "Matching: tên Việt không dấu khớp tên có dấu");
Check(FoodMatchRanker.Rank(viFoods, FoodNameNormalizer.Normalize("Xì dầu"), f => f.Item1, f => f.Item2, _ => 0).First().Item1 == "Nước tương", "Matching: alias Việt có dấu khớp qua chuẩn hoá");
Check(FoodMatchRanker.Rank(new[] { ("Beef", 3), ("Beef", 0) }, "beef", f => f.Item1, _ => [], f => f.Item2).First().Item2 == 0, "Matching: hòa thì ưu tiên nguồn tốt hơn");

var seedNames = new Dictionary<string, string> { ["egg-boiled"] = "Trứng gà luộc", ["beef-lean"] = "Thịt bò nạc nấu chín" };
var catalog = CanonicalCatalogData.Entries(seedNames).ToList();
var aliasOwners = new Dictionary<string, string>();
var clashes = new List<string>();
foreach (var e in catalog)
    foreach (var n in e.Aliases.Select(FoodNameNormalizer.Normalize).Where(n => n.Length > 0).Distinct())
        if (aliasOwners.TryGetValue(n, out var other) && other != e.Key) clashes.Add($"{n}: {other} vs {e.Key}"); else aliasOwners[n] = e.Key;
Check(clashes.Count == 0, $"Catalog: mỗi alias chuẩn hoá chỉ thuộc một canonical ({string.Join("; ", clashes.Take(5))})");
Check(aliasOwners.Count >= 300, $"Catalog: >= 300 alias chuẩn hoá ({aliasOwners.Count})");
Check(CanonicalCatalogData.VietnameseFoods.Length >= 40 && CanonicalCatalogData.VietnameseFoods.Select(v => v.Key).Distinct().Count() == CanonicalCatalogData.VietnameseFoods.Length, $"Catalog: món Việt ({CanonicalCatalogData.VietnameseFoods.Length}), key không trùng");
Check(catalog.Select(e => FoodNameNormalizer.Normalize(e.En)).Distinct().Count() == catalog.Count, "Catalog: tên canonical (chuẩn hoá) không trùng");
Check(CanonicalCatalogData.VietnameseFoods.All(v => v.Kcal >= 0 && v.P + v.C + v.F <= 105 && v.Grams > 0 && v.Kcal <= 902), "Catalog: macro món Việt hợp lý (P+C+F <= 105 g/100 g)");
foreach (var (q, key) in new[] { ("nước mắm", "vn-fish-sauce"), ("Nuoc mam", "vn-fish-sauce"), ("rau muống", "water-spinach"), ("rau muong", "water-spinach"), ("bò xào trứng", "vn-beef-egg-stirfry"), ("bo xao trung", "vn-beef-egg-stirfry"), ("Fish Sauce", "vn-fish-sauce"), ("beef", "beef-lean"), ("eggs", "egg-boiled") })
    Check(aliasOwners.GetValueOrDefault(FoodNameNormalizer.Singular(FoodNameNormalizer.Normalize(q))) == key || aliasOwners.GetValueOrDefault(FoodNameNormalizer.Normalize(q)) == key, $"Catalog: '{q}' -> {key}");

var confirmValidator = new ConfirmAiScanCommandValidator();
Check(confirmValidator.Validate(new ConfirmAiScanCommand(Guid.NewGuid(), [new AiConfirmedIngredient("Beef", Guid.NewGuid(), null)])).IsValid, "Confirm: hợp lệ");
Check(!confirmValidator.Validate(new ConfirmAiScanCommand(Guid.NewGuid(), [new AiConfirmedIngredient("Beef", null, null)])).IsValid && !confirmValidator.Validate(new ConfirmAiScanCommand(Guid.Empty, [])).IsValid, "Confirm: thiếu món/canonical hoặc scanId bị từ chối");
// Khoá cache: "Eggs", "egg", "Nước mắm" / "nuoc mam" cùng khoá → lần resolve sau trúng cache dù FE viết khác
Check(new[] { "Eggs", "egg", "EGG " }.Select(n => FoodNameNormalizer.Singular(FoodNameNormalizer.Normalize(n))).Distinct().Count() == 1 && FoodNameNormalizer.Normalize("Nước mắm") == FoodNameNormalizer.Normalize("nuoc mam"), "Cache: khoá chuẩn hoá gộp biến thể viết hoa/số nhiều/có dấu");

// ALG-03: 21 ngày, 2000 kcal/ngày (2 ngày thiếu), trend 80 → 79.3 (−0.7 kg) → TDEE_raw = 2000 + 0.7×7700/20 = 2269.5
var w0 = new DateOnly(2026, 9, 1);
var win = Enumerable.Range(0, 21).Select(i => new ExpenditureDay(w0.AddDays(i),
    i is 5 or 12 ? null : 2000m,
    i == 0 ? 80m : i == 20 ? 79.3m : (i % 3 == 0 ? 80m - 0.035m * i : null))).ToList();
var raw = ExpenditureCalculator.ComputeRaw(win, 7700);
Check(raw is not null && raw.TdeeRaw == 2269.5m && raw.LoggedDays == 19, $"ALG-03 TDEE_raw = {raw?.TdeeRaw}");
Check(raw!.RateKgPerWeek == -0.245m, $"ALG-03 rate = {raw.RateKgPerWeek} kg/tuần");
Check(ExpenditureCalculator.Smooth(2100, 2269.5m, 0.1m) == 2116.95m, "ALG-03 EMA β=0.1");
Check(ExpenditureCalculator.ComputeRaw(win.Take(1).ToList(), 7700) is null, "ALG-03 thiếu trend → null");
var ok7 = win.Skip(14).Take(7).ToList();
Check(ExpenditureCalculator.Status(w0.AddDays(20), w0, 14, ok7) == ExpenditureStatus.Updating, "ST-01 Updating");
Check(ExpenditureCalculator.Status(w0.AddDays(10), w0, 14, ok7) == ExpenditureStatus.Calibrating, "ST-01 Calibrating < 14 ngày");
var noWeigh = ok7.Select(d => d with { TrendKg = null }).ToList();
Check(ExpenditureCalculator.Status(w0.AddDays(20), w0, 14, noWeigh) == ExpenditureStatus.Holding, "ST-01 Holding không cân");
Check(ExpenditureCalculator.HoldingReason(noWeigh)!.Contains("cân"), "Lý do Holding nêu thiếu cân");

// ALG-04: Coached, rate 0.5 %/tuần, trend 80 → rate 0.4 kg → kcal_raw = 2500 − 0.4×7700/7 = 2060; prev 2300 → clamp −150 → 2150
var prog = new MacroProgram { ProgramType = ProgramType.Coached, WeeklyWeightChangeTargetPercent = 0.5m, ProteinGPerKgBodyweight = 2.0m, DietStyle = DietStyle.Balanced, CalorieFloorKcal = 1400 };
var prevWeek = new MacroProgramWeek { CaloriesTarget = 2300, ProteinTarget = 160m };
var opt4 = new AlgorithmOptions();
var pr = CheckInCalculator.Propose(prog, 80m, 2500m, prevWeek, opt4);
Check(pr.RawCalories == 2060 && pr.NewCalories == 2150 && pr.Clamped && pr.Delta == -150, $"ALG-04 hedge: raw {pr.RawCalories} → {pr.NewCalories}");
Check(pr.Targets.ProteinG == 160m, "ALG-04 protein giữ nguyên (cân đổi ≤ 2 kg)");
Check(CheckInCalculator.Propose(prog, 77m, 2500m, prevWeek, opt4).Targets.ProteinG == 154m, "ALG-04 protein đổi khi cân lệch > 2 kg");
var low = CheckInCalculator.Propose(prog, 80m, 1400m, new MacroProgramWeek { CaloriesTarget = 1450, ProteinTarget = 160m }, opt4);
Check(low.NewCalories == 1400 && low.FloorApplied, $"ALG-04 sàn kcal = {low.NewCalories}");
var (nextD, due) = CheckInCalculator.Next(new DateOnly(2026, 10, 15), DayOfWeek.Monday, new DateOnly(2026, 10, 10), null);
Check(nextD == new DateOnly(2026, 10, 12) && due, $"Check-in kế tiếp {nextD:yyyy-MM-dd} due");
var (next2, due2) = CheckInCalculator.Next(new DateOnly(2026, 10, 15), DayOfWeek.Monday, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12));
Check(next2 == new DateOnly(2026, 10, 19) && !due2, $"Sau check-in 12/10 → kế tiếp {next2:yyyy-MM-dd}, chưa due");
Check(CheckInCalculator.Next(new DateOnly(2026, 10, 17), DayOfWeek.Monday, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12)).IsDue, "Due sớm 2 ngày (17/10 cho 19/10)");
// Goal progress (UC-H04/F06)
Check(GoalProgressCalculator.ProgressPercent(90m, 80m, 85m) == 50m, "Goal progress giảm cân 50 %");
Check(GoalProgressCalculator.ProgressPercent(80m, 90m, 85m) == 50m, "Goal progress tăng cân 50 %");
Check(GoalProgressCalculator.ProgressPercent(90m, 80m, 95m) == 0m, "Goal progress kẹp 0 khi đi ngược");
Check(GoalProgressCalculator.ProgressPercent(90m, 90m, 90m) is null, "Goal progress null khi Start = Target");
var g0 = new DateOnly(2026, 10, 1);
var gPoints = new List<(DateOnly Date, decimal Kg)> { (g0, 90m), (g0.AddDays(7), 89m), (g0.AddDays(14), 88m) };
Check(GoalProgressCalculator.Eta(gPoints, 80m, g0.AddDays(14)) == g0.AddDays(14 + 56), "Goal ETA: −1 kg/tuần, còn 8 kg → 56 ngày");
Check(GoalProgressCalculator.Eta(gPoints, 95m, g0.AddDays(14)) is null, "Goal ETA null khi đi sai hướng");
var gWeeks = GoalProgressCalculator.Weeks(g0, 91m, gPoints, g0.AddDays(14));
Check(gWeeks.Count == 3 && gWeeks[0].DeltaKg == -1m && gWeeks[2].TrendKg == 88m, $"Goal weeks {gWeeks.Count} cột");

// Period: độ dài chu kỳ / kỳ
var periods = new List<PeriodLog> { new() { StartDate = g0, EndDate = g0.AddDays(4) }, new() { StartDate = g0.AddDays(28) } };
PeriodSupport.Recompute(periods);
Check(periods[0].CycleLengthDays is null && periods[0].PeriodLengthDays == 5 && periods[1].CycleLengthDays == 28 && periods[1].PeriodLengthDays is null, "Period recompute");

// Mapster: DailyProgressModel có IsLogged, bỏ qua EstimatedTdeeKcal
Mapster.TypeAdapterConfig.GlobalSettings.Scan(typeof(ExpenditureMappingConfig).Assembly);
var snapModel = Mapster.TypeAdapter.Adapt<DailyProgressModel>(new DailyProgressSnapshot { SnapshotDate = g0, IsLogged = true, CaloriesConsumed = 100m });
Check(snapModel.IsLogged && snapModel.CaloriesConsumed == 100m && snapModel.EstimatedTdeeKcal is null, "Mapster DailyProgressModel");

// Expenditure: Flux Range + Changes
Check(ExpenditureCalculator.FluxHalfWidth(21, 7700, 0.1m) == 37m, "Flux ±37 kcal ở cửa sổ 21 ngày");
Check(ExpenditureCalculator.FluxHalfWidth(3, 7700, 0.1m) == 110m, "Flux rộng hơn khi ít dữ liệu (tối thiểu tính như 7 ngày)");
var tdeeSeries = Enumerable.Range(0, 31).ToDictionary(i => g0.AddDays(i), i => 2400m + i);
var lastDay = g0.AddDays(30);
var c7 = ExpenditureCalculator.Change(tdeeSeries, lastDay, 7, g0);
Check(c7.Status == "Ready" && c7.DeltaKcal == 7m, "Change 7 ngày = +7 kcal");
Check(ExpenditureCalculator.Change(tdeeSeries, lastDay, 90, g0) is { Status: "Waiting", DeltaKcal: null }, "Change 90 ngày Waiting khi chưa đủ lịch sử");
Check(ExpenditureCalculator.Change(tdeeSeries, lastDay, 30, g0.AddDays(5)) is { Status: "Waiting" }, "Change Waiting khi mốc trước ngày bắt đầu tính");

// Quy tắc "ngày đã log" (Food_Log README §2.1)
// "Ngày đã log" = có ≥ 1 entry chưa xoá: LoadDaysAsync lọc ở DB (l.Entries.Any()), kiểm bằng SQL ở scratchpad SqlCheck, không cần DB thật
Check(typeof(FoodLog).GetProperty("Status") is null && typeof(FoodLog).GetProperty("StatusSource") is null, "FoodLog không còn Status/StatusSource (đã gỡ đánh dấu ngày)");
Check(typeof(DayLogResponse).GetProperty("Status") is null && typeof(WeekDayModel).GetProperty("Status") is null, "DayLogResponse/WeekDayModel không còn status");

// Giờ mặc định theo bữa + suy bữa từ giờ (Food_Log README §2.2)
Check(MealTimes.Default(MealType.Dinner) == new TimeOnly(18, 30) && MealTimes.Default(MealType.EveningSnack) == new TimeOnly(21, 0), "Giờ mặc định Dinner 18:30, EveningSnack 21:00");
Check(MealTimes.ForTime(new TimeOnly(9, 59)) == MealType.Breakfast && MealTimes.ForTime(new TimeOnly(10, 0)) == MealType.MorningSnack, "Ranh giới Breakfast/MorningSnack 10:00");
Check(MealTimes.ForTime(new TimeOnly(11, 30)) == MealType.Lunch && MealTimes.ForTime(new TimeOnly(14, 0)) == MealType.AfternoonSnack, "Ranh giới 11:30 Lunch, 14:00 AfternoonSnack");
Check(MealTimes.ForTime(new TimeOnly(17, 0)) == MealType.Dinner && MealTimes.ForTime(new TimeOnly(21, 0)) == MealType.EveningSnack && MealTimes.ForTime(new TimeOnly(2, 0)) == MealType.Breakfast, "Ranh giới 17:00 Dinner, 21:00 EveningSnack, 02:00 Breakfast");
Check(Enum.GetValues<MealType>().All(m => MealTimes.ForTime(MealTimes.Default(m)) == m), "Giờ mặc định của mỗi bữa suy ngược về đúng bữa đó");

// Open Food Facts parser (UC-C08): nutriments tính gram/100 g → mg/µg
using var offDoc = System.Text.Json.JsonDocument.Parse("""
{"code":"3017620422003","product":{"product_name":"Nutella","brands":"Nutella, Ferrero","serving_quantity":"15","serving_size":"15 g",
 "nutriments":{"energy-kcal_100g":539,"proteins_100g":6.3,"carbohydrates_100g":57.5,"fat_100g":30.9,"fiber_100g":0,"sugars_100g":56.3,
 "sodium_100g":0.0428,"saturated-fat_100g":10.6,"vitamin-d_100g":0.0000025}}}
""");
var off = OpenFoodFactsParser.Parse("3017620422003", offDoc.RootElement);
Check(off is { Name: "Nutella", Brand: "Nutella", ServingGrams: 15m, ServingName: "15 g" }, "OFF: tên, brand đầu tiên, khẩu phần 15 g");
Check(off!.Nutrition.CaloriesPer100g == 539m && off.Nutrition.ProteinPer100g == 6.3m && off.Nutrition.FatPer100g == 30.9m, "OFF: macro /100 g");
Check(off.Nutrition.SodiumPer100g == 42.8m && off.Nutrition.VitaminDPer100g == 2.5m, "OFF: sodium g→mg (0.0428 → 42.8), vitamin D g→µg (2.5e-6 → 2.5)");
using var kjDoc = System.Text.Json.JsonDocument.Parse("""{"product":{"product_name":"X","nutriments":{"energy-kj_100g":418.4,"proteins_100g":250}}}""");
var kjProduct = OpenFoodFactsParser.Parse("12345678", kjDoc.RootElement);
Check(kjProduct is not null && kjProduct.Nutrition.CaloriesPer100g == 100m && kjProduct.Nutrition.ProteinPer100g == 100m, "OFF: kJ → kcal (418.4 kJ = 100 kcal), macro kẹp ≤ 100");
using var notFoundDoc = System.Text.Json.JsonDocument.Parse("""{"status":0,"status_verbose":"product not found"}""");
Check(OpenFoodFactsParser.Parse("12345678", notFoundDoc.RootElement) is null, "OFF: status 0 → không có sản phẩm");
using var noNutriDoc = System.Text.Json.JsonDocument.Parse("""{"product":{"product_name":"Y","nutriments":{"sodium_100g":0.1}}}""");
Check(OpenFoodFactsParser.Parse("12345678", noNutriDoc.RootElement) is null, "OFF: không có năng lượng/macro → coi như chưa có dữ liệu");

// Cài đặt Food Log: mặc định, validate, mapping chuỗi
var defaults = Mapster.TypeAdapter.Adapt<FoodLogSettingsModel>(new FoodLogSetting());
Check(defaults == new FoodLogSettingsModel("NoOverages", 7, 23, "Left", "PlusIcon", true, true, "Show", "Daily", false, true, true, "Speed",
    "Standard", true, false, true, "Edit", true, true, true, true, "PlateOnly",
    "Standard", true, false, true, "MultiAdd"), "Cài đặt Food Log: mặc định đúng spec FE (28 trường)");
UpdateFoodLogSettingsCommand Settings(int first = 7, int last = 23) => new(OverageDisplay.NoOverages, first, last, TimelineAlignment.Left, AddFoodTrigger.PlusIcon,
    true, true, WeekBannerMode.Show, CalendarScrollMode.Daily, false, true, true, SearchOptimization.Speed,
    TileDensity.Standard, true, false, true, TileHighlight.Edit,
    true, false, false, false, BannerDisplay.PlateOnly,
    TileDensity.Standard, true, false, true, SearchRowHighlight.MultiAdd);
var settingsValidator = new UpdateFoodLogSettingsCommandValidator();
Check(settingsValidator.Validate(Settings()).IsValid && settingsValidator.Validate(Settings(0, 1)).IsValid && settingsValidator.Validate(Settings(22, 23)).IsValid, "Cài đặt: khoảng giờ hợp lệ (7–23, 0–1, 22–23)");
Check(!settingsValidator.Validate(Settings(23, 7)).IsValid && !settingsValidator.Validate(Settings(10, 10)).IsValid, "Cài đặt: giờ đầu ≥ giờ cuối bị từ chối");
Check(!settingsValidator.Validate(Settings(-1, 5)).IsValid && !settingsValidator.Validate(Settings(7, 24)).IsValid, "Cài đặt: giờ ngoài 0–23 bị từ chối");
Check(!settingsValidator.Validate(Settings() with { Alignment = (TimelineAlignment)9 }).IsValid, "Cài đặt: enum ngoài miền bị từ chối");
var changed = Mapster.TypeAdapter.Adapt<FoodLogSettingsModel>(new FoodLogSetting { WeekBanner = WeekBannerMode.ShowAndPin, ShowBrandedResults = false, Optimization = SearchOptimization.Accuracy });
Check(changed.WeekBanner == "ShowAndPin" && !changed.ShowBrandedResults && changed.Optimization == "Accuracy", "Cài đặt: enum → chuỗi đúng tên");
Check(!settingsValidator.Validate(Settings() with { TimelineTileDensity = (TileDensity)9 }).IsValid && !settingsValidator.Validate(Settings() with { TimelineHighlight = (TileHighlight)9 }).IsValid, "Cài đặt: enum ô món ngoài miền bị từ chối");
var tiles = Mapster.TypeAdapter.Adapt<FoodLogSettingsModel>(new FoodLogSetting { TimelineTileDensity = TileDensity.Compact, TimelineHighlight = TileHighlight.None, TimelineShowWeight = true, TimelineShowMacros = false });
Check(tiles.TimelineTileDensity == "Compact" && tiles.TimelineHighlight == "None" && tiles.TimelineShowWeight && !tiles.TimelineShowMacros && tiles.TimelineShowServing, "Cài đặt: 5 trường ô món → model");
Check(!settingsValidator.Validate(Settings() with { LoggerBannerDisplay = (BannerDisplay)9 }).IsValid, "Cài đặt: enum banner ngoài miền bị từ chối");
var banner = Mapster.TypeAdapter.Adapt<FoodLogSettingsModel>(new FoodLogSetting { LoggerBannerShowCalories = false, LoggerBannerShowProtein = true, LoggerBannerShowFat = false, LoggerBannerShowCarbs = false, LoggerBannerDisplay = BannerDisplay.RemainingAfterPlate });
Check(banner.LoggerBannerDisplay == "RemainingAfterPlate" && banner.LoggerBannerShowProtein && !banner.LoggerBannerShowCalories && !banner.LoggerBannerShowFat && !banner.LoggerBannerShowCarbs, "Cài đặt: 5 trường banner → model");
Check(Mapster.TypeAdapter.Adapt<FoodLogSettingsModel>(new FoodLogSetting()) is { LoggerBannerShowCalories: true, LoggerBannerShowProtein: true, LoggerBannerShowFat: true, LoggerBannerShowCarbs: true }, "Cài đặt: banner mặc định hiện đủ kcal, P, F, C");
Check(!settingsValidator.Validate(Settings() with { LoggerTileDensity = (TileDensity)9 }).IsValid && !settingsValidator.Validate(Settings() with { LoggerHighlight = (SearchRowHighlight)9 }).IsValid, "Cài đặt: enum Logger tiles ngoài miền bị từ chối");
var logger = Mapster.TypeAdapter.Adapt<FoodLogSettingsModel>(new FoodLogSetting { LoggerTileDensity = TileDensity.Comfort, LoggerHighlight = SearchRowHighlight.Energy, LoggerShowWeight = true, LoggerShowMacros = false });
Check(logger.LoggerTileDensity == "Comfort" && logger.LoggerHighlight == "Energy" && logger.LoggerShowWeight && !logger.LoggerShowMacros && logger.LoggerShowServing, "Cài đặt: 5 trường Logger tiles → model");
// Nutrition overview: khoá FE đủ và không trùng
string[] feKeys = ["calories", "protein", "fat", "carbs", "fiber", "netCarbs", "starch", "sugars", "sugarsAdded", "monounsaturated", "polyunsaturated", "omega3", "omega3Ala", "omega3Dha", "omega3Epa", "omega6", "saturated", "transFat",
    "cysteine", "histidine", "isoleucine", "leucine", "lysine", "methionine", "phenylalanine", "threonine", "tryptophan", "tyrosine", "valine",
    "b1", "b2", "b3", "b5", "b6", "b12", "folate", "vitaminA", "vitaminC", "vitaminD", "vitaminE", "vitaminK",
    "calcium", "copper", "iron", "magnesium", "manganese", "phosphorus", "potassium", "selenium", "sodium", "zinc", "alcohol", "caffeine", "cholesterol", "choline", "water"];
var beKeys = NutritionOverviewKeys.ByCode.Values.Append(NutritionOverviewKeys.NetCarbs).ToList();
Check(beKeys.Count == beKeys.Distinct().Count() && feKeys.OrderBy(k => k).SequenceEqual(beKeys.OrderBy(k => k)), $"Overview: 56 khoá khớp danh mục FE ({beKeys.Count})");
Check(!new GetNutritionOverviewQueryValidator().Validate(new GetNutritionOverviewQuery((OverviewRange)9)).IsValid && new GetNutritionOverviewQueryValidator().Validate(new GetNutritionOverviewQuery(OverviewRange.Month, new DateOnly(2026, 10, 1))).IsValid, "Overview: range ngoài miền bị từ chối");
Check(FoodEmoji.For(FoodCategory.Fruits) == "🍎" && FoodEmoji.For(FoodCategory.Other) is null && FoodEmoji.For(null) is null, "Emoji: nhóm → emoji, Other/không có món → null");

var pointModel = Mapster.TypeAdapter.Adapt<ExpenditurePointModel>(new ExpenditureSnapshot { SnapshotDate = g0, EstimatedTdeeKcal = 2429m, Status = ExpenditureStatus.Holding, WindowDays = 21 });
Check(pointModel.EstimatedTdeeKcal == 2429m && pointModel.Status == "Holding" && pointModel.FluxLowKcal is null, "Mapster ExpenditurePointModel (flux gán sau ở handler)");

// AI Plate: nhận dạng ảnh theo magic bytes, parser/normalizer pha 1, matcher + khẩu phần pha 2, validator, 503 khi chưa có khoá
byte[] jpg = [0xFF, 0xD8, 0xFF, 0xE0], png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0], webp = "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();
Check(ImageSniffer.Detect(jpg) == "image/jpeg" && ImageSniffer.Detect(png) == "image/png" && ImageSniffer.Detect(webp) == "image/webp" && ImageSniffer.Detect("GIF89a"u8) is null && ImageSniffer.Detect([]) is null, "AI: nhận dạng jpeg/png/webp, từ chối khác");

using var aiDoc = System.Text.Json.JsonDocument.Parse("""
    {"confidence":"HIGH","dishes":[
      {"name":"Sizzling Beef with Egg","ingredients":[
        {"name":"Beef","grams":"150"},{"name":"  ","grams":5},{"name":"Egg","grams":99999},{"name":"Salt","grams":0}]},
      {"name":"Empty","ingredients":[]}]}
    """);
var aiPlate = AiPlateNormalizer.Normalize(AiMealParser.ParseMeal(aiDoc.RootElement));
Check(aiPlate.Dishes.Count == 1 && aiPlate.Estimated && aiPlate.Confidence == "high", "AI pha 1: bỏ món không nguyên liệu, estimated=true, confidence chuẩn hoá");
var aiDish = aiPlate.Dishes[0];
Check(aiDish.Ingredients.Count == 2 && aiDish.Ingredients[0] is { Name: "Beef", Grams: 150 } && aiDish.Ingredients[1].Grams == 5000, "AI pha 1: bỏ tên rỗng / 0 g, gram dạng chuỗi đọc được, gram chặn 5000");
var aiEst = AiPlateNormalizer.NormalizeEstimate(new AiIngredientEstimate(100, -5, 10, 50));
Check(aiEst.Protein == 0 && aiEst.Calories == 290 && aiEst.Carbs == 50, $"AI ước lượng: chặn âm, kcal lệch Atwater lấy theo macro ({aiEst.Calories})");

Check(AiIngredientMatcher.Words("Soy Sauces").SequenceEqual(["soy", "sauce"]) && AiIngredientMatcher.Words("Eggs").SequenceEqual(["egg"]) && AiIngredientMatcher.Words("Glass").SequenceEqual(["glass"]), "AI pha 2: tách từ, số ít");
Check(AiIngredientMatcher.Tier("Egg", "eggs") == 0 && AiIngredientMatcher.Tier("Egg, whole, raw", "Egg") == 1 && AiIngredientMatcher.Tier("Hard boiled egg", "Egg") == 2 && AiIngredientMatcher.Tier("Eggplant", "Egg") == 1 && AiIngredientMatcher.Tier("Scrambled eggs", "egg") == 3, "AI pha 2: mức khớp tên");
Check(AiIngredientMatcher.SourcePriority(FoodSource.USDA, true, false) < AiIngredientMatcher.SourcePriority(FoodSource.System, false, false)
    && AiIngredientMatcher.SourcePriority(FoodSource.System, false, false) < AiIngredientMatcher.SourcePriority(FoodSource.UserCreated, false, true)
    && AiIngredientMatcher.SourcePriority(FoodSource.UserCreated, false, true) < AiIngredientMatcher.SourcePriority(FoodSource.OpenFoodFacts, false, false), "AI pha 2: ưu tiên nguồn hệ thống đã xác minh → hệ thống → của user → OFF");
var egg50 = new List<(Guid, string, decimal)> { (Guid.NewGuid(), "1 quả", 50) };
var s100 = NutritionCalculator.ChooseServing(100, egg50);
Check(s100.ServingId is not null && s100.Quantity == 2 && s100.WeightGrams == 100 && s100.UnitLabel == "1 quả", "AI pha 2: 100 g trứng = 2 × '1 quả'");
var s5 = NutritionCalculator.ChooseServing(5, egg50);
Check(s5.ServingId is null && s5.Quantity == 5 && s5.UnitLabel == "g" && NutritionCalculator.ChooseServing(30, []).ServingId is null, "AI pha 2: ngoài 0.25–4 khẩu phần hoặc không có khẩu phần → log theo gram");
var spoon = NutritionCalculator.ChooseServing(30, [(Guid.NewGuid(), "1 cup", 240), (Guid.NewGuid(), "1 tbsp", 15)]);
Check(spoon.UnitLabel == "1 tbsp" && spoon.Quantity == 2 && spoon.WeightGrams == 30, "AI pha 2: chọn khẩu phần gần gram nhất");

var aiOpts = Microsoft.Extensions.Options.Options.Create(new AiOptions());
var aiValidator = new AnalyzeMealCommandValidator(aiOpts);
MealPhoto Ph(byte[]? data = null) => new(data ?? jpg);
Check(!aiValidator.Validate(new AnalyzeMealCommand([], null)).IsValid && !aiValidator.Validate(new AnalyzeMealCommand([], "  ")).IsValid, "AI: rỗng cả ảnh lẫn mô tả bị từ chối");
Check(aiValidator.Validate(new AnalyzeMealCommand([], "2 eggs")).IsValid && aiValidator.Validate(new AnalyzeMealCommand([Ph(), Ph(png), Ph(webp)], null)).IsValid, "AI: chỉ mô tả hoặc 3 ảnh hợp lệ");
Check(!aiValidator.Validate(new AnalyzeMealCommand([Ph(), Ph(), Ph(), Ph()], null)).IsValid, "AI: quá 3 ảnh bị từ chối");
Check(!aiValidator.Validate(new AnalyzeMealCommand([Ph("GIF89a"u8.ToArray())], null)).IsValid && !aiValidator.Validate(new AnalyzeMealCommand([Ph([0xFF, 0xD8, 0xFF, .. new byte[5 * 1024 * 1024]])], null)).IsValid, "AI: sai loại / quá 5 MB bị từ chối");
Check(!aiValidator.Validate(new AnalyzeMealCommand([], new string('a', 501))).IsValid && aiValidator.Validate(new AnalyzeMealCommand([], new string('a', 500))).IsValid, "AI: mô tả > 500 ký tự bị từ chối");
var resolveValidator = new ResolveAiIngredientQueryValidator();
Check(resolveValidator.Validate(new ResolveAiIngredientQuery("Beef", 150)).IsValid && !resolveValidator.Validate(new ResolveAiIngredientQuery("", 150)).IsValid && !resolveValidator.Validate(new ResolveAiIngredientQuery("Beef", 0)).IsValid && !resolveValidator.Validate(new ResolveAiIngredientQuery("Beef", 5001)).IsValid, "AI resolve: validator name/gram");

var fakeUow = new FakeUow();
try { await new AnalyzeMealCommandHandler(new StubAnalyzer(), new StubNames(), aiOpts, fakeUow, new FakeUser(), TimeProvider.System).Handle(new AnalyzeMealCommand([], "2 eggs"), default); Check(false, "AI: thiếu khoá phải ném 503"); }
catch (ServiceUnavailableException e) { Check(e.StatusCode == 503 && e.Message == "AI chưa được cấu hình", "AI: thiếu khoá → 503 'AI chưa được cấu hình'"); }
var aiWithKey = await new AnalyzeMealCommandHandler(new StubAnalyzer(), new StubNames(), Microsoft.Extensions.Options.Options.Create(new AiOptions { ApiKey = "k" }), fakeUow, new FakeUser(), TimeProvider.System).Handle(new AnalyzeMealCommand([], "2 eggs"), default);
Check(aiWithKey.Dishes.Count == 1 && aiWithKey.Dishes[0].Ingredients[0].Name == "Egg", "AI: có khoá → gọi analyzer và chuẩn hoá");
var savedScan = fakeUow.Added.OfType<FoodScanAnalysis>().SingleOrDefault();
Check(savedScan is not null && aiWithKey.ScanId == savedScan.Id && savedScan.Status == ScanStatus.Completed && savedScan.Mode == ScanMode.Describe && savedScan.ResultJson!.Contains("Egg") && savedScan.StorageRef is null, "AI scan: lưu kết quả (Completed, không lưu ảnh) và trả ScanId");

// AI Plate Phase 2: dầu/đường/nước chấm ẩn như nguyên liệu thật, bỏ gia vị ≈0 kcal, danh sách tên chuẩn đứng đầu prompt
var promptNames = new[] { "Beef", "Fish Sauce", "Soy Sauce", "Sugar", "Vegetable Oil" };
var promptText = AiMealPrompt.Build(promptNames);
Check(promptText.StartsWith("Preferred ingredient names") && promptText.IndexOf("Beef; Fish Sauce; Soy Sauce; Sugar; Vegetable Oil\n") is var vo && vo > 0 && vo < promptText.IndexOf("Rules:"), "AI prompt: danh sách tên chuẩn đứng ĐẦU, trước phần luật");
Check(promptText == AiMealPrompt.Build(promptNames) && !AiMealPrompt.Build([]).Contains("Preferred ingredient names") && AiMealPrompt.Build([]).StartsWith("You identify"), "AI prompt: ổn định giữa các lần gọi (prompt cache); danh sách rỗng thì không có phần tên");
Check(new[] { "xào", "kho", "nướng tẩm", "xôi", "Butter", "dressing", "ít dầu", "không đường", "Do NOT list salt, pepper, MSG", "boiled, steamed, raw" }.All(promptText.Contains) && promptText.Contains("oil 5 / 10 / 15 g") && promptText.Contains("takes priority over the photos"), "AI prompt: luật theo cách nấu + gram tròn + bỏ gia vị + mô tả của user thắng ảnh");
Check(!promptText.Contains("kcal\"") && promptText.Contains("Do NOT output calories or macros"), "AI prompt: AI vẫn không trả calo/macro (schema analyze không đổi)");

var seasoned = AiPlateNormalizer.Normalize(new AiMealDraft([new AiDraftDish("Xôi thịt gà",
    [new("Sticky Rice", 200), new("Chicken", 80), new("Vegetable Oil", 10), new("Soy Sauce", 15), new("Salt", 2), new("Black Pepper", 1), new("MSG", 1), new("Hạt nêm", 3), new("Muối", 2), new("Bell Pepper", 30)])], "medium"));
var seasonedNames = seasoned.Dishes[0].Ingredients.Select(i => i.Name).ToList();
Check(seasonedNames.SequenceEqual(["Sticky Rice", "Chicken", "Vegetable Oil", "Soy Sauce", "Bell Pepper"]), "AI Phase 2: muối/tiêu/bột ngọt/hạt nêm bị lọc, dầu + nước tương + ớt chuông giữ nguyên");
Check(AiPlateNormalizer.IsZeroCalorieSeasoning("  SALT ") && AiPlateNormalizer.IsZeroCalorieSeasoning("Monosodium Glutamate") && !AiPlateNormalizer.IsZeroCalorieSeasoning("Salted Butter") && !AiPlateNormalizer.IsZeroCalorieSeasoning("Chili Pepper"), "AI Phase 2: lọc theo nguyên tên, 'Salted Butter'/'Chili Pepper' không bị chặn");
var onlySalt = AiPlateNormalizer.Normalize(new AiMealDraft([new AiDraftDish("Water", [new("Salt", 5)])], "high"));
Check(onlySalt.Dishes.Count == 0, "AI Phase 2: món chỉ có gia vị ≈0 kcal bị bỏ cả món");

// ===== Check-in v2 + target theo ngày (CheckIn_Flow 4b–4d, NewProgram_Flow) =====
Mapster.TypeAdapterConfig.GlobalSettings.Scan(typeof(ProgramMappingConfig).Assembly);
var ciStart0 = new DateOnly(2026, 10, 10); // Thứ 7
var (nxEarly, dueEarly) = CheckInCalculator.Next(new DateOnly(2026, 10, 18), DayOfWeek.Monday, ciStart0, new DateOnly(2026, 10, 17)); // check-in sớm Thứ 7
Check(nxEarly == new DateOnly(2026, 10, 26) && !dueEarly, "Check-in v2: check-in sớm Thứ 7 cho Thứ 2 → kỳ kế Thứ 2 tuần SAU, hôm sau không due (hết lỗi check-in 3 lần liên tiếp)");
Check(CheckInCalculator.Next(new DateOnly(2026, 10, 20), DayOfWeek.Monday, ciStart0, new DateOnly(2026, 10, 19)).NextDate == new DateOnly(2026, 10, 26)
    && CheckInCalculator.Next(new DateOnly(2026, 10, 24), DayOfWeek.Monday, ciStart0, new DateOnly(2026, 10, 21)).NextDate == new DateOnly(2026, 10, 26)
    && CheckInCalculator.Next(new DateOnly(2026, 10, 12), DayOfWeek.Monday, ciStart0, null).NextDate == new DateOnly(2026, 10, 12), "Check-in v2: đúng hạn / trễ / lần đầu không đổi so với trước");
Check(CheckInCalculator.Next(new DateOnly(2026, 10, 24), DayOfWeek.Monday, ciStart0, new DateOnly(2026, 10, 19)).IsDue, "Check-in v2: due từ 2 ngày trước ngày check-in (giữ cửa sổ sớm ≤ 2 ngày)");
var prPlain = pr with { Clamped = false, FloorApplied = false };
Check(CheckInCalculator.ReasonCodes(prPlain, ExpenditureStatus.Calibrating, null, 0.5m, 0).SequenceEqual(["CALIBRATING"])
    && CheckInCalculator.ReasonCodes(prPlain, ExpenditureStatus.Holding, 0.3m, 0.5m, 2).SequenceEqual(["HOLDING", "INCOMPLETE_DAYS"])
    && CheckInCalculator.ReasonCodes(prPlain with { Clamped = true, FloorApplied = true }, ExpenditureStatus.Updating, 0.8m, 0.5m, 0).SequenceEqual(["FASTER_THAN_TARGET", "CLAMPED", "FLOOR_APPLIED"])
    && CheckInCalculator.ReasonCodes(prPlain, ExpenditureStatus.Updating, 0.2m, 0.5m, 0).SequenceEqual(["SLOWER_THAN_TARGET"])
    && CheckInCalculator.ReasonCodes(prPlain, ExpenditureStatus.Updating, 0.5m, 0.5m, 0).SequenceEqual(["ON_TARGET"]), "Check-in v2: reason code (CALIBRATING/HOLDING/INCOMPLETE_DAYS/FASTER/SLOWER/ON_TARGET/CLAMPED/FLOOR)");
// Ngày incomplete bị bỏ qua: ngày KcalIn = null không vào trung bình và không tính là ngày đã log
var incDays = new[] { new ExpenditureDay(new DateOnly(2026, 10, 1), 2200, 80m), new ExpenditureDay(new DateOnly(2026, 10, 2), null, null), new ExpenditureDay(new DateOnly(2026, 10, 3), 2400, 80m) };
Check(ExpenditureCalculator.ComputeRaw(incDays, 7700)!.AvgKcalIn == 2300m && ExpenditureCalculator.ComputeRaw(incDays, 7700)!.LoggedDays == 2, "Check-in v2: ngày KcalIn = null (đánh dấu incomplete) không vào trung bình/số ngày log");

// Target hiệu lực của ngày: DayTarget thắng, không có thì target tuần + dịch kcal, ngày nhịn ăn = 0
var wkTarget = new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = new DateOnly(2026, 10, 12), CaloriesTarget = 2000, ProteinTarget = 150m, CarbsTarget = 200m, FatTarget = 66.7m, AdjustmentReason = AdjustmentReason.Initial };
var progDays = new MacroProgram
{
    ProgramType = ProgramType.Collaborative, CalorieShiftMode = CalorieShiftMode.Even,
    DayTargets =
    [
        new MacroProgramDayTarget { DayOfWeek = DayOfWeek.Thursday, CaloriesTarget = 2500, ProteinTarget = 160m, CarbsTarget = 300m, FatTarget = 70m, IsLocked = true },
        new MacroProgramDayTarget { DayOfWeek = DayOfWeek.Friday, IsFastingDay = true },
    ],
};
Check(ProgramTargetResolver.ForDate(wkTarget, progDays, new DateOnly(2026, 10, 15)).CaloriesTarget == 2500 && ProgramTargetResolver.ForDate(wkTarget, progDays, new DateOnly(2026, 10, 14)).CaloriesTarget == 2000, "DayTarget: ngày có dòng dùng dòng đó, ngày khác dùng target tuần");
Check(ProgramTargetResolver.ForDate(wkTarget, progDays, new DateOnly(2026, 10, 16)) is { CaloriesTarget: 0, ProteinTarget: 0, CarbsTarget: 0, FatTarget: 0 }, "DayTarget: ngày nhịn ăn = 0 mọi macro");
var progShift = new MacroProgram { ProgramType = ProgramType.Coached, CalorieShiftMode = CalorieShiftMode.TrainingDays, CalorieShiftPercent = 10m, CalorieShiftDays = DaysOfWeek.Saturday | DaysOfWeek.Sunday };
Check(ProgramTargetResolver.ForDate(wkTarget, progShift, new DateOnly(2026, 10, 17)).CaloriesTarget == 2200 && ProgramTargetResolver.ForDate(wkTarget, progShift, new DateOnly(2026, 10, 14)).CaloriesTarget == 1920, "DayTarget: không có dòng thì vẫn áp dịch kcal của program (Shift)");
var resolvedDays = ProgramTargetResolver.Days(wkTarget, progDays);
Check(resolvedDays.Count == 7 && resolvedDays[0].DayOfWeek == "Monday" && resolvedDays[6].DayOfWeek == "Sunday" && resolvedDays[3] is { Calories: 2500, IsLocked: true } && resolvedDays[4].Calories == 0 && !resolvedDays[0].IsLocked, "GetCurrentProgram days[7]: Thứ 2 → CN, có cờ khoá và ngày nhịn ăn");

// Chia lại 7 ngày khi target tuần đổi (check-in Collaborative Accept): tổng = 7 × target, ngày khoá giữ nguyên, protein giữ nguyên
MacroProgram CollabProg(int kcal = 2000)
{
    var p = new MacroProgram { ProgramType = ProgramType.Collaborative };
    p.DayTargets = ProgramDayTargets.FromInputs(p.Id, ProgramTargetResolver.WeekOrder.Select(d => new DayTargetInput(d, kcal, 150m, (kcal - 600) * 0.4m / 4m, (kcal - 600) * 0.6m / 9m)));
    return p;
}
var rebProg = CollabProg();
rebProg.DayTargets.First(d => d.DayOfWeek == DayOfWeek.Saturday).IsLocked = true;
var rebWeek = new MacroProgramWeek { CaloriesTarget = 2100, ProteinTarget = 150m, CarbsTarget = 200m, FatTarget = 66.7m };
ProgramDayTargets.Rebalance(rebProg, rebWeek);
var rebSum = rebProg.DayTargets.Sum(d => d.CaloriesTarget!.Value);
Check(rebSum == 14700 && rebProg.DayTargets.First(d => d.DayOfWeek == DayOfWeek.Saturday).CaloriesTarget == 2000 && rebProg.DayTargets.Where(d => d.DayOfWeek != DayOfWeek.Saturday).All(d => d.CaloriesTarget is >= 2115 and <= 2120), "Rebalance: ngân sách mới 7×2100, ngày khoá giữ 2000, 6 ngày còn lại chia đều phần còn lại (tổng đúng)");
Check(rebProg.DayTargets.All(d => d.ProteinTarget == 150m) && rebWeek.CaloriesTarget == 2100 && rebProg.DayTargets.Where(d => !d.IsLocked).All(d => Math.Abs(4m * d.ProteinTarget!.Value + 4m * d.CarbsTarget!.Value + 9m * d.FatTarget!.Value - d.CaloriesTarget!.Value) <= 6m), "Rebalance: protein giữ nguyên, macro mỗi ngày vẫn khớp kcal ngày, target tuần = trung bình 7 ngày");
var rebNoop = CollabProg(); rebNoop.DayTargets.First().CaloriesTarget = 2003; // tổng 14003, trung bình làm tròn = 2000 → 7 × 2000 lệch 3 = sai số làm tròn
var rebNoopWeek = new MacroProgramWeek { CaloriesTarget = 2000, ProteinTarget = 150m, CarbsTarget = 200m, FatTarget = 66.7m };
ProgramDayTargets.Rebalance(rebNoop, rebNoopWeek);
Check(rebNoop.DayTargets.Count(d => d.CaloriesTarget == 2000) == 6 && rebNoop.DayTargets.Count(d => d.CaloriesTarget == 2003) == 1 && rebNoopWeek.CaloriesTarget == 2000, "Rebalance: lệch ≤ sai số làm tròn của trung bình thì không chia lại (Reject giữ nguyên 7 ngày)");
var rebFasting = CollabProg(); rebFasting.DayTargets.First(d => d.DayOfWeek == DayOfWeek.Sunday).IsFastingDay = true;
ProgramDayTargets.Rebalance(rebFasting, new MacroProgramWeek { CaloriesTarget = 1800, ProteinTarget = 150m, CarbsTarget = 200m, FatTarget = 66.7m });
Check(rebFasting.DayTargets.Sum(d => d.IsFastingDay ? 0 : d.CaloriesTarget!.Value) == 12600 && rebFasting.DayTargets.First(d => d.IsFastingDay).CaloriesTarget == 2000, "Rebalance: ngày nhịn ăn không nhận phần chia lại");

// Wizard New Program: trần, kiểm tra, preview + cảnh báo
var optW = new AlgorithmOptions();
Check(ProgramDraftCalculator.Limits(2500, optW).KcalMax == 5000 && ProgramDraftCalculator.Limits(1000, optW).KcalMax == 3000 && ProgramDraftCalculator.Limits(2500, optW).ProteinPerKgMax == 4m, "Wizard: trần kcal = 2 × TDEE (làm tròn 100, tối thiểu 3000), protein ≤ 4 g/kg");
try { ProgramDraftCalculator.Validate([new DayTargetInput(DayOfWeek.Monday, 20000, 100, 100, 50)], ProgramDraftCalculator.Limits(2500, optW), 80m); Check(false, "Wizard: 20000 kcal phải bị chặn"); }
catch (BadRequestException) { Check(true, "Wizard: nhập 20000 kcal bị chặn (lỗi của MacroFactor)"); }
try { ProgramDraftCalculator.Validate([new DayTargetInput(DayOfWeek.Monday, 2000, 400, 100, 50)], ProgramDraftCalculator.Limits(2500, optW), 80m); Check(false, "Wizard: protein quá trần phải bị chặn"); }
catch (BadRequestException) { Check(true, "Wizard: protein > 4 g/kg bị chặn"); }
var even7 = ProgramTargetResolver.WeekOrder.Select(d => new DayTargetInput(d, 2000, 150m, 200m, 66.7m)).ToList();
var pv = ProgramDraftCalculator.Preview(even7, 2300, 80m, 75m, null, 7700, new DateOnly(2026, 10, 19));
Check(pv.AverageCalories == 2000 && pv.WeeklyBudgetKcal == 14000 && pv.WeeklyChangeKg == -0.27m && pv.ProjectedEndDate is not null && pv.Warnings.Count == 0 && pv.MacroKcalAverage == 2000, "Wizard preview: TB 2000 kcal, ngân sách 14000, giảm ~0.27 kg/tuần suy từ TDEE, ngày dự kiến, không cảnh báo");
var badDay = even7.ToList(); badDay[2] = new DayTargetInput(DayOfWeek.Wednesday, 2000, 100m, 100m, 20m);
var pvBad = ProgramDraftCalculator.Preview(badDay, 2300, 80m, null, 2100, 7700, new DateOnly(2026, 10, 19));
Check(pvBad.Warnings.Any(w => w.Code == "MACRO_KCAL_MISMATCH" && w.DayOfWeek == "Wednesday") && pvBad.Warnings.Count(w => w.Code == "BELOW_FLOOR") == 7, "Wizard preview: cảnh báo macro lệch kcal (1180 vs 2000) và dưới sàn kcal, không chặn");
var cpv = new CreateProgramCommandValidator();
CreateProgramCommand Cpc(ProgramType t, List<DayTargetInput>? d) => new(GoalType.WeightLoss, 0.5m, t, DietStyle.Balanced, ProteinLevel.Moderate, null, null, null, DayTargets: d);
Check(!cpv.Validate(Cpc(ProgramType.Manual, null)).IsValid && cpv.Validate(Cpc(ProgramType.Manual, even7)).IsValid && !cpv.Validate(Cpc(ProgramType.Coached, even7)).IsValid
    && cpv.Validate(Cpc(ProgramType.Collaborative, null)).IsValid && cpv.Validate(Cpc(ProgramType.Collaborative, even7)).IsValid
    && !cpv.Validate(Cpc(ProgramType.Collaborative, [.. even7.Take(6), even7[0]])).IsValid && !cpv.Validate(Cpc(ProgramType.Collaborative, even7.Take(3).ToList())).IsValid, "CreateProgram: Manual bắt buộc 7 ngày, Coached không có, Collaborative tuỳ chọn; trùng thứ/thiếu ngày bị từ chối");

// ===== Edit Goal: loại goal suy ra, dải tốc độ, Manual, đổi hướng ⇒ requiresNewProgram =====
var egToday = new DateOnly(2026, 10, 19);
GoalEditState EgState(GoalType type = GoalType.WeightLoss, decimal? target = 75m, ProgramType prog = ProgramType.Coached, decimal rate = 0.5m)
    => new(80m, type, target, new DateOnly(2026, 10, 1), prog, rate, 1500, 2500);
Check(GoalEditCalculator.Derive(80m, 75m) == GoalDirection.Loss && GoalEditCalculator.Derive(80m, 85m) == GoalDirection.Gain
    && GoalEditCalculator.Derive(80m, 80.05m) == GoalDirection.Maintain && GoalEditCalculator.Derive(80m, null) == GoalDirection.Maintain, "Edit Goal: loại goal suy ra (đích thấp hơn = Loss, cao hơn = Gain, bằng/trống = Maintain)");
var egSame = GoalEditCalculator.Compute(EgState(), 72m, 0.6m, optW, egToday);
Check(!egSame.Preview.RequiresNewProgram && egSame.Preview.GoalType == "WeightLoss" && egSame.Preview.WeeklyRatePercent == -0.6m && egSame.Preview.WeeklyRateKg == -0.48m
    && egSame.Preview.MonthlyRateKg == -1.92m && egSame.Preview.MonthlyRatePercent == -2.4m && egSame.Preview.RateBand == "Standard", "Edit Goal: sửa tại chỗ (cùng hướng Loss) → rate có dấu âm, tuần/tháng, dải Standard");
Check(egSame.Preview.InitialDailyBudgetKcal == MacroTargetCalculator.DailyCalories(2500, 0.6m, 80m, 7700, 1500) && egSame.Preview.ProjectedEndDate == egToday.AddDays(117)
    && egSame.ProgramRatePercent == 0.6m && egSame.SignedWeeklyKg == -0.48m, "Edit Goal: ngân sách khởi điểm cùng công thức ProgramWeekBuilder, ngày dự kiến = hôm nay + |đích−cân| ÷ rate (lạc quan)");
Check(egSame.Preview.Changes.Weight is { From: 75m, To: 72m, Changed: true } && egSame.Preview.Changes.Rate is { From: -0.5m, To: -0.6m, Changed: true }
    && GoalEditCalculator.Compute(EgState(), 75m, null, optW, egToday).Preview.Changes is { Weight.Changed: false, Rate.Changed: false }, "Edit Goal: changes từ → tới, 'No Change' khi giữ nguyên (rate null = giữ tốc độ hiện tại)");
var egGain = GoalEditCalculator.Compute(EgState(), 85m, null, optW, egToday);
Check(egGain.Preview is { RequiresNewProgram: true, GoalTypeChanged: true, GoalType: "WeightGain", CurrentGoalType: "WeightLoss" } && egGain.Preview.WeeklyRatePercent == GoalEditCalculator.Bounds(GoalDirection.Gain, optW).DefaultPercent
    && egGain.Preview.StartDate == egToday, "Edit Goal: đổi Loss → Gain ⇒ requiresNewProgram, rate mặc định của dải Gain, startDate = hôm nay");
var egMaintain = GoalEditCalculator.Compute(EgState(), null, null, optW, egToday);
Check(egMaintain.Preview is { RequiresNewProgram: true, GoalType: "Maintenance", WeeklyRatePercent: 0, RateBand: "None", ProjectedEndDate: null, InitialDailyBudgetKcal: 2500 }, "Edit Goal: bỏ đích (Loss → Maintenance) ⇒ requiresNewProgram, rate 0, ngân sách = TDEE, không có ngày dự kiến");
var egMaintainSame = GoalEditCalculator.Compute(EgState(GoalType.Maintenance, 80m, rate: 0m), 80.05m, 0.7m, optW, egToday);
Check(!egMaintainSame.Preview.RequiresNewProgram && egMaintainSame.Preview.WeeklyRatePercent == 0 && egMaintainSame.Preview.GoalType == "Maintenance", "Edit Goal: Maintenance giữ Maintenance → sửa tại chỗ, rate luôn 0 dù gửi 0.7");
Check(GoalEditCalculator.Compute(EgState(GoalType.MuscleGain, 82m, rate: -0.2m), 85m, 0.2m, optW, egToday).Preview is { RequiresNewProgram: false, GoalType: "MuscleGain" }, "Edit Goal: MuscleGain cùng hướng tăng giữ nguyên loại, không buộc tạo program mới");
var egManual = GoalEditCalculator.Compute(EgState(prog: ProgramType.Manual), 72m, 0.4m, optW, egToday);
Check(egManual.Preview is { InitialDailyBudgetKcal: null, IsManualProgram: true, RequiresNewProgram: false } && GoalEditCalculator.Compute(EgState(prog: ProgramType.Collaborative), 72m, 0.4m, optW, egToday).Preview.InitialDailyBudgetKcal is not null, "Edit Goal: Manual không có ngân sách tự động (null), Collaborative có");
try { GoalEditCalculator.Compute(EgState(), 72m, 1.2m, optW, egToday); Check(false, "Edit Goal: rate vượt Fast phải bị từ chối"); }
catch (BadRequestException) { Check(true, "Edit Goal: giảm cân 1.2%/tuần vượt dải Fast (1%) bị từ chối"); }
try { GoalEditCalculator.Compute(EgState(GoalType.WeightGain, 85m, rate: -0.3m), 88m, 0.6m, optW, egToday); Check(false, "Edit Goal: tăng cân 0.6%/tuần phải bị từ chối"); }
catch (BadRequestException) { Check(true, "Edit Goal: tăng cân 0.6%/tuần vượt dải Fast của Gain (0.5%) bị từ chối"); }
try { GoalEditCalculator.Compute(EgState(), 200m, 0.5m, optW, egToday); Check(false, "Edit Goal: đích quá xa phải bị từ chối"); }
catch (BadRequestException) { Check(true, "Edit Goal: đích lệch > 50% cân hiện tại bị từ chối"); }
Check(GoalEditCalculator.Compute(EgState(rate: 1.5m), 72m, null, optW, egToday).Preview.WeeklyRatePercent == -1m, "Edit Goal: tốc độ cũ vượt dải (legacy 1.5%) được kẹp về Fast khi không chỉnh rate");
var egVal = new EditGoalCommandValidator();
// ===== Danh mục tiếng Anh (reseed-foods) =====
Check(FoodData.VietnameseNames.Keys.All(FoodEnglish.SeedNames.ContainsKey) && FoodEnglish.SeedNames.Count == FoodData.VietnameseNames.Count && FoodEnglish.SeedNames.Values.All(n => !FoodEnglish.HasVietnamese(n)) && FoodEnglish.SeedNames.Values.Distinct().Count() == FoodEnglish.SeedNames.Count,
    "Reseed: đủ 60 tên Anh cho món seed, không dấu, không trùng");
var viServings = new[] { "1 quả", "1/2 chén", "1 muỗng canh", "1 muỗng cà phê", "1 chén múi", "1 củ vừa", "5 trái", "1 đĩa", "1 ổ", "1 phần vừa" };
Check(viServings.Select(FoodEnglish.ServingName).SequenceEqual(["1 piece", "1/2 cup", "1 tbsp", "1 tsp", "1 cup segments", "1 medium", "5 piece", "1 plate", "1 loaf", "1 medium serving"])
    && FoodEnglish.ServingName("1 tbsp") == "1 tbsp" && FoodEnglish.ServingName("100 g") == "100 g" && viServings.All(s => !FoodEnglish.HasVietnamese(FoodEnglish.ServingName(s))), "Reseed: đổi khẩu phần Việt → Anh, khẩu phần đã Anh giữ nguyên");
Check(CanonicalCatalogData.VietnameseFoods.All(v => !FoodEnglish.HasVietnamese(FoodEnglish.ServingName(v.Serving)) && !FoodEnglish.HasVietnamese(v.En)) && CanonicalCatalogData.Condiments.All(c => !FoodEnglish.HasVietnamese(c.Name) && !FoodEnglish.HasVietnamese(c.Canonical)), "Reseed: mọi khẩu phần món VN đổi được sang Anh; tên món và canonical gia vị đều tiếng Anh");
var viFlagBefore = CanonicalCatalogData.IncludeVietnamese;
try
{
    CanonicalCatalogData.IncludeVietnamese = false;
    var enOnly = CanonicalCatalogData.Entries(FoodData.VietnameseNames).ToList();
    var viNormsT = CanonicalCatalogData.VietnameseAliasNorms();
    Check(enOnly.All(e => e.Vi is null && e.Aliases.All(a => !FoodEnglish.HasVietnamese(a))) && enOnly.SelectMany(e => e.Aliases).All(a => !viNormsT.Contains(FoodNameNormalizer.Normalize(a))) && enOnly.All(e => !FoodEnglish.HasVietnamese(e.En)),
        "Reseed: chế độ English-only: không NameVi, không alias có dấu hay dạng không dấu của tên Việt");
    Check(enOnly.Any(e => e.Aliases.Any(a => FoodNameNormalizer.Normalize(a) == "oatmeal")) && enOnly.Any(e => e.Aliases.Any(a => FoodNameNormalizer.Normalize(a) == "pho")) && enOnly.Any(e => e.Aliases.Any(a => FoodNameNormalizer.Normalize(a) == "fish sauce")),
        "Reseed: English-only vẫn giữ alias Anh (oatmeal, fish sauce) và tên món Việt quen dùng trên thực đơn Anh (pho)");
    CanonicalCatalogData.IncludeVietnamese = true;
    var withVi = CanonicalCatalogData.Entries(FoodData.VietnameseNames).ToList();
    Check(withVi.Any(e => e.Aliases.Any(a => FoodNameNormalizer.Normalize(a) == "yen mach")) && withVi.Any(e => e.Vi is not null), "Reseed: bật IncludeVietnamese vẫn seed alias Việt như cũ (mặc định không đổi hành vi)");
}
finally { CanonicalCatalogData.IncludeVietnamese = viFlagBefore; }
Check(IconKeyResolver.Resolve("Oats, dry", FoodCategory.Grains) == "oatmeal_bowl" && IconKeyResolver.Resolve("Egg, hard-boiled", FoodCategory.Protein) == "boiled_egg" && IconKeyResolver.Resolve("Beef pho", FoodCategory.FastFood) == "pho", "Reseed: icon theo tên Anh với bộ icon 529 khoá (oats, trứng luộc, phở có icon riêng)");

// ===== (d) Shortcuts, Full Body, Progress Photos =====
var scDefaults = ShortcutCatalog.Defaults();
Check(scDefaults.Count == 12 && scDefaults.Take(4).Select(s => s.Key).SequenceEqual(["search", "barcode", "ai", "weight"]) && scDefaults.Take(8).All(s => s.Enabled) && scDefaults.Skip(8).All(s => !s.Enabled)
    && scDefaults.Select(s => s.Key).Distinct().Count() == 12, "Shortcuts: mặc định 12 khoá, 8 bật (Search, Barcode, AI, Weight đứng đầu), 4 tắt");
var scVal = new UpdateShortcutsCommandValidator();
UpdateShortcutsCommand Sc(ToolbarStyle style, params ShortcutItemModel[] items) => new(style, [.. items]);
var okItems = new[] { new ShortcutItemModel("weight", true, "blue"), new ShortcutItemModel("search", true, null), new ShortcutItemModel("ai", true, "red") };
Check(scVal.Validate(Sc(ToolbarStyle.SearchBarcodeCustom, okItems)).IsValid && scVal.Validate(Sc(ToolbarStyle.SearchAndBarcode, okItems[0])).IsValid, "Shortcuts: danh sách hợp lệ (thứ tự tuỳ ý, thiếu khoá thì BE bổ sung)");
Check(!scVal.Validate(Sc(ToolbarStyle.SearchAndBarcode, new ShortcutItemModel("nope", true, null))).IsValid
    && !scVal.Validate(Sc(ToolbarStyle.SearchAndBarcode, new ShortcutItemModel("search", true, null), new ShortcutItemModel("search", true, null))).IsValid, "Shortcuts: khoá lạ / trùng khoá bị từ chối");
Check(!scVal.Validate(Sc(ToolbarStyle.SearchAndBarcode, new ShortcutItemModel("search", true, "pink"))).IsValid && scVal.Validate(Sc(ToolbarStyle.SearchAndBarcode, new ShortcutItemModel("search", true, "purple"))).IsValid
    && ShortcutCatalog.Colors.Count == 7, "Shortcuts: màu ngoài bảng 7 màu bị từ chối");
Check(!scVal.Validate(Sc(ToolbarStyle.SearchAndBarcode, new ShortcutItemModel("search", false, null))).IsValid && !scVal.Validate(Sc(ToolbarStyle.SearchAndBarcode)).IsValid, "Shortcuts: không bật cái nào / rỗng bị từ chối");
Check(!scVal.Validate(Sc(ToolbarStyle.FourCustom, okItems)).IsValid && scVal.Validate(Sc(ToolbarStyle.FourCustom, [.. okItems, new ShortcutItemModel("metrics", true, null)])).IsValid && !scVal.Validate(Sc((ToolbarStyle)99, okItems)).IsValid, "Shortcuts: Four Custom cần ≥ 4 shortcut bật; kiểu toolbar sai bị từ chối");
var scNorm = ShortcutCatalog.Normalize(okItems);
Check(scNorm.Count == 12 && scNorm.Take(3).Select(s => s.Key).SequenceEqual(["weight", "search", "ai"]) && scNorm.Skip(3).All(s => !s.Enabled && s.Color is null), "Shortcuts: giữ thứ tự user, bổ sung khoá thiếu ở cuối (tắt)");
Check(ShortcutCatalog.Deserialize("{hỏng").Count == 12 && ShortcutCatalog.Deserialize(null).Count == 12 && ShortcutCatalog.Deserialize(ShortcutCatalog.Serialize(scNorm)).Select(s => (s.Key, s.Enabled, s.Color)).SequenceEqual(scNorm.Select(s => (s.Key, s.Enabled, s.Color))), "Shortcuts: JSON hỏng → mặc định; serialize/deserialize khứ hồi giữ nguyên");

var fbVal = new UpsertFullBodyCommandValidator(TimeProvider.System);
var tapeOk = new TapeMeasurementsModel(38, 120, null, 100, 82, 95, 33, 33.5m, 28, 28, 17, 17, 58, 58, 38, 38, 22, 22);
Check(fbVal.Validate(new UpsertFullBodyCommand(null, 15m, tapeOk)).IsValid && fbVal.Validate(new UpsertFullBodyCommand(null, null, null)).IsValid && tapeOk.HasAnyValue, "Full Body: validator chấp nhận sheet hợp lệ / sheet rỗng");
Check(!fbVal.Validate(new UpsertFullBodyCommand(null, 1m, null)).IsValid && !fbVal.Validate(new UpsertFullBodyCommand(null, 80m, null)).IsValid && !fbVal.Validate(new UpsertFullBodyCommand(null, null, tapeOk with { LeftAnkleCm = 2 })).IsValid
    && !fbVal.Validate(new UpsertFullBodyCommand(null, null, tapeOk with { ShouldersCm = 400 })).IsValid, "Full Body: % mỡ ngoài 2–75, số đo ngoài 5–300 cm bị từ chối");
Check(new TapeMeasurementsModel(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null).HasAnyValue == false, "Full Body: tape toàn null = không có hàng Tape");

// Ảnh tiến độ: bóc metadata mà không nén lại, giữ Orientation
byte[] B(params object[] parts) => parts.SelectMany(p => p switch { byte[] a => a, string s => System.Text.Encoding.Latin1.GetBytes(s), byte b1 => new[] { b1 }, int n => new[] { (byte)n }, _ => throw new InvalidOperationException() }).ToArray();
byte[] JpegSeg(byte marker, byte[] payload) => B((byte)0xFF, marker, (byte)((payload.Length + 2) >> 8), (byte)((payload.Length + 2) & 0xFF), payload);
var exifPayload = B("Exif\0\0", "II", 0x2A, 0, 8, 0, 0, 0, /*2 entry*/ 2, 0, /*Orientation=6*/ 0x12, 0x01, 3, 0, 1, 0, 0, 0, 6, 0, 0, 0, /*GPS ptr*/ 0x25, 0x88, 4, 0, 1, 0, 0, 0, 0x40, 0, 0, 0, 0, 0, 0, 0, "GPSMARKER");
var sosTail = B((byte)0xFF, (byte)0xDA, 0, 8, 1, 1, 0, 0, 0x3F, 0, "IMAGEDATA", (byte)0xFF, (byte)0xD9);
var jpgIn = B((byte)0xFF, (byte)0xD8, JpegSeg(0xE0, B("JFIF\0", 1, 1, 0, 0, 1, 0, 1, 0, 0)), JpegSeg(0xE1, exifPayload), JpegSeg(0xFE, B("secretcomment")), JpegSeg(0xED, B("Photoshop 3.0")), sosTail);
Check(ImageSniffer.Detect(jpgIn) == "image/jpeg", "Ảnh: magic bytes JPEG nhận đúng");
var jpgOut = ImageMetadataStripper.Strip(jpgIn, "image/jpeg");
var jpgText = System.Text.Encoding.Latin1.GetString(jpgOut);
Check(!jpgText.Contains("GPSMARKER") && !jpgText.Contains("secretcomment") && !jpgText.Contains("Photoshop") && jpgText.Contains("JFIF") && jpgText.Contains("IMAGEDATA") && jpgOut.AsSpan(^sosTail.Length).SequenceEqual(sosTail), "Ảnh: JPEG bỏ EXIF/GPS, comment, IPTC; giữ JFIF và dữ liệu ảnh nguyên vẹn");
Check(jpgOut.AsSpan().IndexOf(new byte[] { 0x01, 0x12, 0x00, 0x03, 0, 0, 0, 1, 0x00, 6 }) > 0 && jpgOut.Length < jpgIn.Length, "Ảnh: JPEG giữ hướng xoay (Orientation = 6) bằng EXIF tối thiểu, không còn tag nào khác");
var pngChunk = (string type, byte[] data) => B((byte)(data.Length >> 24), (byte)(data.Length >> 16), (byte)(data.Length >> 8), (byte)data.Length, type, data, 0, 0, 0, 0);
var pngIn = B(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, pngChunk("IHDR", new byte[13]), pngChunk("tEXt", B("Comment\0secretpng")), pngChunk("eXIf", B("GPSMARKER")), pngChunk("IDAT", B("PIXELS")), pngChunk("IEND", []));
var pngText = System.Text.Encoding.Latin1.GetString(ImageMetadataStripper.Strip(pngIn, "image/png"));
Check(!pngText.Contains("secretpng") && !pngText.Contains("GPSMARKER") && !pngText.Contains("eXIf") && pngText.Contains("PIXELS") && pngText.Contains("IEND"), "Ảnh: PNG bỏ tEXt/eXIf, giữ IHDR/IDAT/IEND");
byte[] WebpChunk(string cc, byte[] data) => B(cc, (byte)data.Length, (byte)(data.Length >> 8), 0, 0, data, data.Length % 2 == 1 ? new byte[] { 0 } : Array.Empty<byte>());
var webpBody = B("WEBP", WebpChunk("VP8X", B(0x0C, 0, 0, 0, 0, 0, 0, 0, 0, 0)), WebpChunk("VP8 ", B("PIXL")), WebpChunk("EXIF", B("GPSMARKER")), WebpChunk("XMP ", B("<x:xmp>secret</x:xmp>")));
var webpIn = B("RIFF", (byte)webpBody.Length, (byte)(webpBody.Length >> 8), 0, 0, webpBody);
var webpOut = ImageMetadataStripper.Strip(webpIn, "image/webp");
var webpText = System.Text.Encoding.Latin1.GetString(webpOut);
Check(ImageSniffer.Detect(webpIn) == "image/webp" && !webpText.Contains("EXIF") && !webpText.Contains("XMP ") && !webpText.Contains("GPSMARKER") && webpText.Contains("PIXL") && webpOut[20] == 0 && (webpOut[4] | webpOut[5] << 8) == webpOut.Length - 8, "Ảnh: WebP bỏ chunk EXIF/XMP, xoá cờ VP8X, kích thước RIFF khớp");
try { ImageMetadataStripper.Strip(B((byte)0xFF, (byte)0xD8, (byte)0x12, (byte)0x34), "image/jpeg"); Check(false, "Ảnh: JPEG hỏng phải bị từ chối"); }
catch (BadRequestException) { Check(true, "Ảnh: JPEG hỏng bị từ chối"); }
try { ImageMetadataStripper.Strip(B("GIF89a....."), "image/gif"); Check(false, "Ảnh: GIF phải bị từ chối"); }
catch (BadRequestException) { Check(true, "Ảnh: loại khác JPEG/PNG/WebP bị từ chối"); }
var phVal = new UpsertProgressPhotoCommandValidator(Microsoft.Extensions.Options.Options.Create(new ProgressPhotoOptions()), TimeProvider.System);
Check(phVal.Validate(new UpsertProgressPhotoCommand(null, PhotoAngle.Front, jpgIn, true)).IsValid && !phVal.Validate(new UpsertProgressPhotoCommand(null, PhotoAngle.Other, jpgIn, true)).IsValid
    && !phVal.Validate(new UpsertProgressPhotoCommand(null, PhotoAngle.Back, [], true)).IsValid && !phVal.Validate(new UpsertProgressPhotoCommand(null, PhotoAngle.Side, new byte[10 * 1024 * 1024 + 1], true)).IsValid, "Ảnh: validator chỉ nhận Front/Side/Back, không rỗng, ≤ 10 MB");
var phId = Guid.NewGuid(); var phOwner = Guid.NewGuid(); var phNow = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero); var phExp = phNow.AddMinutes(15).ToUnixTimeSeconds();
var phSig = ProgressPhotoUrlSigner.Sign("secret-secret-secret-secret-secret-1", phId, phOwner, phExp);
Check(ProgressPhotoUrlSigner.Verify("secret-secret-secret-secret-secret-1", phId, phOwner, phExp, phSig, phNow) && ProgressPhotoUrlSigner.Verify("secret-secret-secret-secret-secret-1", phId, phOwner, phExp, phSig.ToUpperInvariant(), phNow)
    && !ProgressPhotoUrlSigner.Verify("secret-secret-secret-secret-secret-1", phId, Guid.NewGuid(), phExp, phSig, phNow) && !ProgressPhotoUrlSigner.Verify("secret-secret-secret-secret-secret-1", Guid.NewGuid(), phOwner, phExp, phSig, phNow)
    && !ProgressPhotoUrlSigner.Verify("secret-secret-secret-secret-secret-1", phId, phOwner, phExp + 1, phSig, phNow) && !ProgressPhotoUrlSigner.Verify("khac-khac-khac-khac-khac-khac-1", phId, phOwner, phExp, phSig, phNow)
    && !ProgressPhotoUrlSigner.Verify("secret-secret-secret-secret-secret-1", phId, phOwner, phExp, phSig, phNow.AddMinutes(16)) && !ProgressPhotoUrlSigner.Verify("secret-secret-secret-secret-secret-1", phId, phOwner, phExp, null, phNow) && !ProgressPhotoUrlSigner.Verify("secret-secret-secret-secret-secret-1", phId, phOwner, phExp, "abc", phNow),
    "Ảnh: URL ký gắn với ảnh + chủ ảnh + hạn; sai chủ/ảnh/hạn/khoá/chữ ký hoặc hết hạn đều bị từ chối");

Check(egVal.Validate(new EditGoalCommand(72m, 0.5m)).IsValid && egVal.Validate(new EditGoalCommand(null, null)).IsValid && !egVal.Validate(new EditGoalCommand(10m, null)).IsValid && !egVal.Validate(new EditGoalCommand(72m, 2m)).IsValid && !egVal.Validate(new EditGoalCommand(72m, -0.1m)).IsValid, "Edit Goal: validator đích 20–400 kg, rate 0–1.5");
// Nutrition Calculation Engine: gram/khẩu phần, co giãn, làm tròn ToEven, cộng gộp, Atwater, target
Check(NutritionCalculator.Scale(12.5m, 100) == 12.5m && NutritionCalculator.Scale(12.5m, 150) == 18.75m && NutritionCalculator.Scale(12.5m, 0) == 0m && NutritionCalculator.Scale(12.5m, -5) == 0m, "Engine: Scale 100/150/0 g, gram âm = 0");
Check(NutritionRounding.Round(0.125m, 2) == 0.12m && NutritionRounding.Round(0.135m, 2) == 0.14m && NutritionCalculator.Scale(12.5m, 1) == 0.12m && Math.Round(0.125m, 2) == NutritionRounding.Round(0.125m, 2), "Engine: làm tròn ToEven, trùng Math.Round mặc định của snapshot cũ");
Check(NutritionCalculator.Grams(2, 150) == 300 && NutritionCalculator.Grams(50, null) == 50 && NutritionCalculator.Grams(0, 150) == 0 && NutritionCalculator.Grams(2, 0) == 0 && NutritionCalculator.Grams(-1, null) == 0, "Engine: gram từ khẩu phần / gram trực tiếp / dữ liệu hỏng");
Check(NutritionCalculator.ToGrams(250, QuantityUnit.Milliliter, 1.03m) == 257.5m && NutritionCalculator.ToGrams(1, QuantityUnit.Liter, 0.92m) == 920m && NutritionCalculator.ToGrams(2, QuantityUnit.Kilogram) == 2000 && NutritionCalculator.ToGrams(1, QuantityUnit.Pound) == 453.59237m, "Engine: ml → g theo tỉ trọng, kg, lb");
Check(NutritionCalculator.ToGrams(250, QuantityUnit.Milliliter) is null && NutritionCalculator.ToGrams(250, QuantityUnit.Milliliter, 0m) is null && NutritionCalculator.ToGrams(100, QuantityUnit.Gram) == 100 && NutritionCalculator.ToGrams(0, QuantityUnit.Milliliter) == 0, "Engine: chưa biết tỉ trọng → null, không đoán 1 g/ml");

IReadOnlyDictionary<string, NutrientValue> Prof(params (string Code, decimal Amount, NutrientAmountStatus Status)[] xs) => xs.ToDictionary(x => x.Code, x => new NutrientValue(x.Amount, x.Status));
var beefP = Prof(("energy_kcal", 250, NutrientAmountStatus.Measured), ("sodium_mg", 60, NutrientAmountStatus.Measured));
var eggP = Prof(("energy_kcal", 155, NutrientAmountStatus.Measured), ("sodium_mg", 0, NutrientAmountStatus.Measured), ("fiber_g", 0, NutrientAmountStatus.Measured));
var sumP = NutritionCalculator.SumProfiles([(beefP, 150m), (eggP, 50m)]);
Check(sumP["energy_kcal"].Amount == 452.5m && sumP["energy_kcal"].Status == NutrientAmountStatus.Measured, "Engine: cộng nhiều nguyên liệu 150 g bò + 50 g trứng = 452.5 kcal");
Check(sumP["sodium_mg"].Amount == 90m && sumP["sodium_mg"].Status == NutrientAmountStatus.Measured, "Engine: giá trị 0 là đã biết, vẫn Measured");
Check(sumP["fiber_g"].Status == NutrientAmountStatus.Estimated && !sumP.ContainsKey("zinc_mg"), "Engine: chất thiếu ở một nguyên liệu → Estimated, chất không ai có thì vắng mặt");
Check(NutritionCalculator.ScaleNutrient(eggP, "sodium_mg", 1) == 0m && NutritionCalculator.ScaleNutrient(beefP, "fiber_g", 1) is null && NutritionCalculator.ScaleNutrient(null, "fiber_g", 1) is null, "Engine: 0 khác thiếu (null)");
Check(NutritionCalculator.SumProfiles([(Prof(("iron_mg", 1, NutrientAmountStatus.Estimated)), 100m), (Prof(("iron_mg", 1, NutrientAmountStatus.Measured)), 100m)])["iron_mg"].Status == NutrientAmountStatus.Estimated, "Engine: có nguyên liệu Estimated → tổng Estimated");
var scaledP = NutritionCalculator.ScaleProfile(sumP, 0.5m);
Check(scaledP["energy_kcal"].Amount == 226.25m && scaledP["fiber_g"].Status == NutrientAmountStatus.Estimated && scaledP.Count == sumP.Count, "Engine: ScaleProfile giữ trạng thái và khoá");

var perServingP = NutritionCalculator.PerServing(sumP, 2);
Check(perServingP["energy_kcal"].Amount == 226.25m && NutritionCalculator.PerServing(100m, 3) == 33.33m && NutritionCalculator.PerServing(100m, 0) == 100m, "Engine: recipe trên 1 khẩu phần, số khẩu phần ≤ 0 coi là 1");
decimal driftSum = 0; for (var i = 0; i < 30; i++) driftSum += NutritionCalculator.Scale(165m, 33.33m);
Check(Math.Abs(driftSum - NutritionCalculator.Scale(165m, 33.33m * 30)) <= 30 * 0.005m, "Engine: cộng 30 phần đã làm tròn lệch ≤ n × 0.005 so với tính một lần");
decimal ratioV = 100m; for (var i = 0; i < 20; i++) { ratioV = NutritionCalculator.Rescale(ratioV, 1.1m); ratioV = NutritionCalculator.Rescale(ratioV, 1m / 1.1m); }
Check(Math.Abs(ratioV - 100m) <= 0.5m && NutritionCalculator.Rescale((decimal?)null, 2m) is null, "Engine: đổi tỉ lệ lặp lại không trôi quá 0.5, null giữ null");

Check(NutritionCalculator.AveragePerLoggedDay(300, 3) == 100m && NutritionCalculator.AveragePerLoggedDay(10, 0) == 10m && NutritionCalculator.AveragePerLoggedDay(null, 2) == 0m && NutritionCalculator.AveragePerLoggedDay(100, 3) == 33.33m, "Engine: trung bình mỗi ngày đã log, không chia cho 0");

var okEnergy = NutritionCalculator.ValidateEnergy(130, 2.7m, 28, 0.3m);
Check(okEnergy.IsConsistent && okEnergy.ExpectedKcal == 125.5m && okEnergy.DifferenceKcal == 4.5m && !NutritionCalculator.ValidateEnergy(300, 2.7m, 28, 0.3m).IsConsistent && NutritionCalculator.ValidateEnergy(0, 0, 0, 0).IsConsistent, "Engine: Atwater 4P+4C+9F, lệch quá ngưỡng là không nhất quán, macro 0 không đánh giá");
Check(NutritionCalculator.ValidateEnergy(150, 1, 13, 0, 14).IsConsistent && !NutritionCalculator.ValidateEnergy(150, 1, 13, 0).IsConsistent && NutritionCalculator.ReconcileKcal(150, 1, 13, 0, 14) == 150 && NutritionCalculator.ReconcileKcal(150, 1, 13, 0) == 56, "Engine: có dữ liệu cồn thì tính 7 kcal/g, không thì lấy theo macro");
Check(NutritionCalculator.ReconcileKcal(300, 2.7m, 28, 0.3m) == 126 && NutritionCalculator.SnapshotMacros(300, 2.7m, 28, 0.3m, null, 100).Calories == 300m, "Engine: chỉ ReconcileKcal đổi kcal ở biên AI; snapshot món giữ kcal của DB dù lệch macro");
Check(NutritionCalculator.NetCarbs(10, 15) == 0 && NutritionCalculator.NetCarbs(10, null) is null && NutritionCalculator.NetCarbs(30, 5) == 25 && NutritionCalculator.NetCarbs(10, 0) == 10, "Engine: net carbs không âm; chưa biết fiber → null (không coi fiber = 0), fiber = 0 đã biết thì net = carbs");
Check(NutritionCalculator.AveragePerLoggedDayOrNull(null, 3) is null && NutritionCalculator.AveragePerLoggedDayOrNull(0m, 3) == 0m && NutritionCalculator.AveragePerLoggedDayOrNull(30m, 3) == 10m && NutritionCalculator.AveragePerLoggedDayOrNull(7m, 0) == 7m,
    "Engine: trung bình ngày: chưa có tổng → null (khác 0), tổng 0 đã biết → 0, không chia cho 0");

// --- Pipeline chất dinh dưỡng: ánh xạ số USDA (Foundation dùng số khác), chất dẫn xuất, thiếu ≠ 0 ---
UsdaNutrientValue Nv(string number, string unit, decimal amount) => new(number, unit, amount);
var canonical = UsdaFoodMapper.Canonical([Nv("208", "kcal", 100m), Nv("269.3", "g", 4m), Nv("293", "g", 2m), Nv("205.2", "g", 20m), Nv("298", "g", 5m)]);
var primaryWins = UsdaFoodMapper.Canonical([Nv("291", "g", 3m), Nv("293", "g", 9m), Nv("269", "g", 1m), Nv("269.3", "g", 8m)]);
Check(canonical.Single(n => n.Number == "269").Amount == 4m && canonical.Single(n => n.Number == "291").Amount == 2m && canonical.Single(n => n.Number == "205").Amount == 20m && canonical.Single(n => n.Number == "204").Amount == 5m
    && primaryWins.Single(n => n.Number == "291").Amount == 3m && primaryWins.Single(n => n.Number == "269").Amount == 1m, "USDA: số thay thế của Foundation (269.3/293/205.2/298) được đổi về số chuẩn khi thiếu, không ghi đè số chuẩn đã có");
var mapDefs = new Dictionary<string, UsdaFoodMapper.Definition> { ["291"] = new(Guid.NewGuid(), "fiber_g", "g"), ["418"] = new(Guid.NewGuid(), "vitamin_b12_mcg", "mcg"), ["307"] = new(Guid.NewGuid(), "sodium_mg", "mg"), ["320"] = new(Guid.NewGuid(), "vitamin_a_mcg", "mcg") };
var (usdaAmounts, usdaMapped) = UsdaFoodMapper.MapNutrients(new UsdaFood(1, "x", "Foundation", null, [], [Nv("293", "g", 2.5m), Nv("418", "µg", 1.25m), Nv("307", "mg", 300m), Nv("320", "IU", 500m), Nv("268", "kJ", 400m), Nv("307", "g", 9m)]), mapDefs);
Check(usdaMapped.Count == 3 && usdaMapped.Single(m => m.Code == "vitamin_b12_mcg").Amount == 1.25m && usdaMapped.Single(m => m.Code == "fiber_g").Amount == 2.5m && usdaMapped.Single(m => m.Code == "sodium_mg").Amount == 300m
    && !usdaMapped.Any(m => m.Code == "vitamin_a_mcg") && usdaAmounts.Count == 3, "USDA: µg = mcg, đơn vị lạ (IU cho định nghĩa µg, kJ) bị bỏ, số trùng chỉ lấy một lần, 293 thành fiber khi thiếu 291");
NutrientValue Est(decimal a) => new(a, NutrientAmountStatus.Measured);
var omegaProfile = new Dictionary<string, NutrientValue> { ["omega3_ala_g"] = Est(0.5m), ["omega3_epa_g"] = Est(0.1m), ["omega3_dha_g"] = Est(0.2m), ["pufa_22_5_n3_g"] = Est(0.05m), ["pufa_18_2_n6_cc_g"] = Est(3m), ["pufa_20_4_g"] = Est(0.1m) };
NutrientDerivation.Apply(omegaProfile);
Check(omegaProfile["omega3_g"].Amount == 0.85m && omegaProfile["omega3_g"].Status == NutrientAmountStatus.Estimated && omegaProfile["omega6_g"].Amount == 3.1m,
    "Dẫn xuất: omega-3 = ALA+EPA+DHA+DPA, omega-6 = LA + 18:3 n-6 + 20:2/20:3 n-6 + ARA khi thiếu tổng, trạng thái Estimated");
var keepTotals = new Dictionary<string, NutrientValue> { ["omega3_g"] = Est(9m), ["omega3_ala_g"] = Est(1m), ["vitamin_d_mcg"] = Est(2m), ["vitamin_a_mcg"] = Est(50m), ["retinol_mcg"] = Est(1000m) };
NutrientDerivation.Apply(keepTotals);
Check(keepTotals["omega3_g"].Amount == 9m && keepTotals["omega3_g"].Status == NutrientAmountStatus.Measured && keepTotals["vitamin_d_mcg"].Amount == 2m && keepTotals["vitamin_a_mcg"].Amount == 50m && !keepTotals.ContainsKey("omega6_g"),
    "Dẫn xuất: không bao giờ ghi đè chất tổng đã có; không có phần nào thì không bịa chất tổng (omega-6 vắng mặt)");
var viaCarotenoids = new Dictionary<string, NutrientValue> { ["retinol_mcg"] = Est(100m), ["carotene_beta_mcg"] = Est(1200m), ["carotene_alpha_mcg"] = Est(240m), ["cryptoxanthin_beta_mcg"] = Est(240m) };
NutrientDerivation.Apply(viaCarotenoids);
Check(viaCarotenoids["vitamin_a_mcg"].Amount == 100m + 1200m / 12m + 480m / 24m && viaCarotenoids["vitamin_a_mcg"].Status == NutrientAmountStatus.Estimated && !viaCarotenoids.ContainsKey("vitamin_d_mcg"),
    "Dẫn xuất: vitamin A RAE = retinol + β-carotene/12 + (α-carotene + β-cryptoxanthin)/24; không quy đổi IU vitamin D (mọi món có D đều có µg)");
// SR Legacy/FNDDS chỉ có 619 (18:3), 618 (18:2), 620 (20:4) chưa phân loại → ALA/LA/AA fallback; 855 (AA n-6) thắng 620
var srOnly = new Dictionary<string, NutrientValue> { ["pufa_18_3_g"] = Est(0.3m), ["omega3_epa_g"] = Est(0.1m), ["pufa_18_2_g"] = Est(10m), ["pufa_20_4_g"] = Est(0.2m) };
NutrientDerivation.Apply(srOnly);
Check(srOnly["omega3_ala_g"].Amount == 0.3m && srOnly["omega3_ala_g"].Status == NutrientAmountStatus.Estimated && srOnly["omega3_g"].Amount == 0.4m && srOnly["omega6_g"].Amount == 10.2m,
    "Dẫn xuất: ALA = 851 ?? 619, LA = 675 ?? 618, AA = 855 ?? 620 (SR chỉ có dạng chưa phân loại)");
var withAa = new Dictionary<string, NutrientValue> { ["pufa_18_2_n6_cc_g"] = Est(10m), ["pufa_18_2_g"] = Est(99m), ["pufa_20_4_n6_g"] = Est(0.05m), ["pufa_20_4_g"] = Est(0.2m), ["omega3_ala_g"] = Est(1m), ["pufa_18_3_g"] = Est(5m), ["pufa_20_3_n3_g"] = Est(0.02m) };
NutrientDerivation.Apply(withAa);
Check(withAa["omega6_g"].Amount == 10.05m && withAa["omega3_ala_g"].Amount == 1m && withAa["omega3_ala_g"].Status == NutrientAmountStatus.Measured && withAa["omega3_g"].Amount == 1.02m,
    "Dẫn xuất: số phân loại (675/855/851) thắng số chưa phân loại (không cộng đôi), omega-3 cộng cả 20:3 n-3");
// Folate hiển thị là DFE: có 435 thì dùng, Foundation chỉ có 417 thì giữ 417
var folateBoth = new Dictionary<string, NutrientValue> { ["folate_mcg"] = Est(350m), ["folate_dfe_mcg"] = Est(520m) };
var folateOnlyTotal = new Dictionary<string, NutrientValue> { ["folate_mcg"] = Est(350m) };
NutrientDerivation.Apply(folateBoth); NutrientDerivation.Apply(folateOnlyTotal);
Check(folateBoth["folate_mcg"].Amount == 520m && folateOnlyTotal["folate_mcg"].Amount == 350m, "Dẫn xuất: folate = DFE (435) khi có, thiếu thì dùng folate tổng (417)");

// --- DRI theo hồ sơ: bảng tĩnh, ưu tiên user > DRI > seed ở NutrientCatalog ---
DriRange? Dri(string code, Gender g, int? age, decimal? kcal = null) => DriTable.Resolve(code, g, age, kcal, out _);
Check(Dri("iron_mg", Gender.Female, 25) is { Target: 18, Ceiling: 45 } && Dri("vitamin_a_mcg", Gender.Female, 25) is { Target: 700, Ceiling: null }, "DRI: nữ 25 tuổi sắt 18 (UL 45), vitamin A 700 (không trần vì UL chỉ cho retinol)");
Check(Dri("iron_mg", Gender.Female, 55) is { Target: 8 } && Dri("calcium_mg", Gender.Female, 55) is { Target: 1200, Ceiling: 2000 } && Dri("vitamin_b6_mg", Gender.Female, 55) is { Target: 1.5m }, "DRI: nữ 55 tuổi sắt 8, canxi 1200 (UL 2000), B6 1.5");
Check(Dri("vitamin_b6_mg", Gender.Male, 60) is { Target: 1.7m, Ceiling: 100 } && Dri("calcium_mg", Gender.Male, 60) is { Target: 1000, Ceiling: 2000 } && Dri("calcium_mg", Gender.Male, 75) is { Target: 1200 } && Dri("vitamin_d_mcg", Gender.Male, 75) is { Target: 20 }, "DRI: nam 60 B6 1.7 + canxi 1000; nam 75 canxi 1200 + vitamin D 20");
Check(Dri("magnesium_mg", Gender.Male, 25) is { Target: 400, Ceiling: null } && Dri("magnesium_mg", Gender.Male, 40) is { Target: 420 } && Dri("magnesium_mg", Gender.Female, 25) is { Target: 310 } && Dri("magnesium_mg", Gender.Female, 40) is { Target: 320 }, "DRI: magiê 400/420 (nam), 310/320 (nữ), không trần (UL chỉ dạng bổ sung)");
Check(Dri("folate_mcg", Gender.Male, 30) is { Target: 400, Ceiling: null } && Dri("vitamin_b3_mg", Gender.Male, 30) is { Target: 16, Ceiling: null } && Dri("vitamin_e_mg", Gender.Male, 30) is { Ceiling: null } && Dri("vitamin_c_mg", Gender.Female, 30) is { Target: 75, Ceiling: 2000 }, "DRI: UL chỉ dạng bổ sung (folate, niacin, vitamin E) không thành trần; vitamin C UL 2000 áp cho tổng");
Check(Dri("fiber_g", Gender.Male, 30, 2400) is { Floor: null, Target: 33.6m } && Dri("fat_saturated_g", Gender.Male, 30, 2400) is { Ceiling: 26.7m, Target: null } && Dri("sugars_added_g", Gender.Male, 30, 2400) is { Ceiling: 60m }, "DRI theo kcal: 2400 kcal → fiber 33.6 g, béo bão hoà ≤ 26.7 g, đường thêm ≤ 60 g");
Check(Dri("fiber_g", Gender.Male, 30) is null && Dri("fat_saturated_g", Gender.Male, 30, 0) is null, "DRI theo kcal: chưa có kcal mục tiêu → null (rơi về mức seed)");
Check(Dri("sodium_mg", Gender.Male, 30) is { Floor: null, Target: 1500, Ceiling: 2300 } && Dri("water_g", Gender.Female, 30) is { Target: 2700 } && Dri("water_g", Gender.Male, 30) is { Target: 3700 } && Dri("caffeine_mg", Gender.Male, 30) is { Ceiling: 400, Target: null }, "DRI: natri AI 1500 + trần 2300, nước 3.7/2.7 L, caffeine trần 400");
Check(Dri("omega3_ala_g", Gender.Male, 30) is { Target: 1.6m } && Dri("omega3_ala_g", Gender.Female, 30) is { Target: 1.1m } && Dri("omega6_g", Gender.Male, 30) is { Target: 17 } && Dri("omega6_g", Gender.Female, 60) is { Target: 11 }, "DRI: ALA 1.6/1.1 g, omega-6 17/12 g (19–50), 14/11 g (51+)");
Check(Dri("omega3_epa_g", Gender.Male, 30) is { IsNone: true } && Dri("leucine_g", Gender.Male, 30) is { IsNone: true } && Dri("cholesterol_mg", Gender.Male, 30) is { IsNone: true }, "DRI: EPA, amino acid, cholesterol không có mức nào");
Check(Dri("sfa_4_0_g", Gender.Male, 30) is null && Dri("energy_kcal", Gender.Male, 30) is null, "DRI: chất không thuộc bảng (chi tiết USDA, macro của chương trình) → null");
var refIron = DriTable.Resolve("iron_mg", Gender.Male, null, null, out var refIronFlag);
var refCalcium = DriTable.Resolve("calcium_mg", Gender.Female, null, null, out _);
DriTable.Resolve("iron_mg", Gender.Female, 25, null, out var knownFlag);
DriTable.Resolve("iron_mg", Gender.Female, 15, null, out var minorFlag);
DriTable.Resolve("iron_mg", Gender.Other, 30, null, out var otherFlag);
Check(refIron is { Target: 18, Ceiling: 45 } && refIronFlag && refCalcium is { Target: 1200, Ceiling: 2000 } && !knownFlag && minorFlag && otherFlag,
    "DRI: thiếu ngày sinh / dưới 19 tuổi / giới Other → mức tham chiếu (Target cao nhất, trần thấp nhất, cờ isReference); đủ hồ sơ thì không");
Check(DriTable.AgeOn(null, new DateOnly(2026, 6, 15)) is null && DriTable.AgeOn(new DateOnly(2000, 6, 15), new DateOnly(2026, 6, 14)) == 25 && DriTable.AgeOn(new DateOnly(2000, 6, 15), new DateOnly(2026, 6, 15)) == 26
    && DriTable.Bucket(30) == 0 && DriTable.Bucket(31) == 1 && DriTable.Bucket(50) == 1 && DriTable.Bucket(51) == 2 && DriTable.Bucket(70) == 2 && DriTable.Bucket(71) == 3, "DRI: tính tuổi theo ngày sinh; nhóm tuổi 19–30 / 31–50 / 51–70 / 71+");
var emptyProfile = new Dictionary<string, NutrientValue>();
NutrientDerivation.Apply(emptyProfile);
Check(emptyProfile.Count == 0, "Dẫn xuất: hồ sơ rỗng vẫn rỗng (thiếu dữ liệu không thành 0)");
Check(NutritionCalculator.PercentOfTarget(50, 0) is null && NutritionCalculator.PercentOfTarget(50, null) is null && NutritionCalculator.PercentOfTarget(null, 100) is null && NutritionCalculator.PercentOfTarget(50, 200) == 25m && NutritionCalculator.PercentOfTarget(1, 3) == 33.3m, "Engine: % so target, target 0 / thiếu → null");
Check(NutritionCalculator.Remaining(2000, 2300) == -300m && NutritionCalculator.Remaining(null, 5) is null && NutritionCalculator.WithinRange(50, 40, 60) && !NutritionCalculator.WithinRange(30, 40, null) && !NutritionCalculator.WithinRange(70, null, 60) && NutritionCalculator.WithinRange(5, null, null), "Engine: còn lại (âm = vượt) và nằm trong khoảng floor/ceiling");

var dayTotals = NutritionCalculator.SumEntries(log.Entries);
Check(dayTotals.Calories == log.TotalCalories && dayTotals.Protein == log.TotalProtein && dayTotals.Carbs == log.TotalCarbs && dayTotals.Fat == log.TotalFat && dayTotals.Fiber == log.TotalFiber && dayTotals.Calories == 455m, "Engine: tổng ngày bỏ entry đã xoá, khớp RecalcTotals");
Check(dayTotals.Calories == e1.Calories + e2.Calories && dayTotals.Fiber == (e1.Fiber ?? 0) + (e2.Fiber ?? 0), "Engine: tổng ngày = tổng snapshot đã lưu của từng entry");

// Nhất quán Food Log ↔ AI resolve ↔ recipe: cùng gram thì cùng snapshot
foreach (var wantGrams in new[] { 300m, 137m, 5m })
{
    var pick = NutritionCalculator.ChooseServing(wantGrams, [(bowl.Id, bowl.ServingName, bowl.WeightInGrams)]);
    var logged = new FoodLogEntry(); FoodLogSupport.Snapshot(logged, rice, FoodLogSupport.Grams(pick.Quantity, pick.ServingId is null ? null : bowl), riceProfile);
    var viaEngine = NutritionCalculator.SnapshotMacros(rice.CaloriesPer100g, rice.ProteinPer100g, rice.CarbsPer100g, rice.FatPer100g, riceProfile,
        NutritionCalculator.Grams(pick.Quantity, pick.ServingId is null ? null : bowl.WeightInGrams));
    Check(viaEngine.Calories == logged.Calories && viaEngine.Protein == logged.Protein && viaEngine.Carbs == logged.Carbs && viaEngine.Fat == logged.Fat && viaEngine.Fiber == logged.Fiber,
        $"Engine: AI resolve khớp Food Log ở {wantGrams} g ({pick.Quantity} × {pick.UnitLabel} = {viaEngine.Calories} kcal)");
}
var engineRecipeServing = new Dictionary<string, NutrientValue> { ["energy_kcal"] = new(226.25m, NutrientAmountStatus.Estimated), ["protein_g"] = new(10.123m, NutrientAmountStatus.Measured), ["fiber_g"] = new(1.005m, NutrientAmountStatus.Measured) };
var engineRecipeEntry = new FoodLogEntry(); FoodLogEntryFactory.SnapshotColumns(engineRecipeEntry, engineRecipeServing, 1.5m);
var recipeEngine = NutritionCalculator.SnapshotFromProfile(engineRecipeServing, 1.5m);
Check(recipeEngine.Calories == engineRecipeEntry.Calories && recipeEngine.Protein == engineRecipeEntry.Protein && recipeEngine.Carbs == 0m && recipeEngine.Fiber == engineRecipeEntry.Fiber && recipeEngine.Sodium is null && engineRecipeEntry.Sodium is null, "Engine: snapshot recipe khớp FoodLogEntryFactory, macro thiếu = 0, sodium thiếu = null");

// Food Detail: GET /foods/{id} trả đủ 56 khoá FE trên 100 g, thiếu = null, 0 vẫn là 0, netCarbs = carbs − fiber
var feKeyed = FoodNutrientStore.ByFeKey(new Dictionary<string, NutrientValue>
{
    ["energy_kcal"] = new(130m, NutrientAmountStatus.Measured), ["carbs_g"] = new(30m, NutrientAmountStatus.Measured), ["fiber_g"] = new(5m, NutrientAmountStatus.Measured),
    ["vitamin_c_mg"] = new(12m, NutrientAmountStatus.Measured), ["sodium_mg"] = new(0m, NutrientAmountStatus.Measured),
});
Check(feKeyed.Count == feKeys.Length && feKeyed.Keys.OrderBy(k => k).SequenceEqual(feKeys.OrderBy(k => k)), $"Food Detail: đủ {feKeyed.Count} khoá FE");
Check(feKeyed["calories"] == 130m && feKeyed["vitaminC"] == 12m && feKeyed["sodium"] == 0m && feKeyed["vitaminA"] is null && feKeyed["protein"] is null, "Food Detail: thiếu = null, 0 vẫn là 0");
Check(feKeyed["netCarbs"] == 25m && FoodNutrientStore.ByFeKey(new Dictionary<string, NutrientValue>())["netCarbs"] is null, "Food Detail: netCarbs = carbs − fiber, chưa có carbs thì null");

// Plate: xem trước dinh dưỡng khớp snapshot khi log thật, cộng nhiều dòng, validator
var plateProfile = new Dictionary<string, NutrientValue>
{
    ["energy_kcal"] = new(rice.CaloriesPer100g, NutrientAmountStatus.Measured), ["protein_g"] = new(rice.ProteinPer100g, NutrientAmountStatus.Measured),
    ["carbs_g"] = new(rice.CarbsPer100g, NutrientAmountStatus.Measured), ["fat_g"] = new(rice.FatPer100g, NutrientAmountStatus.Measured),
    ["fiber_g"] = new(0.4m, NutrientAmountStatus.Measured), ["vitamin_c_mg"] = new(12.3456m, NutrientAmountStatus.Measured),
};
var plateLogged = new FoodLogEntry(); FoodLogSupport.Snapshot(plateLogged, rice, 137m, plateProfile);
var plateAmounts = NutritionCalculator.SnapshotAmounts(plateProfile, NutritionCalculator.GramFactor(137m));
Check(plateAmounts["energy_kcal"] == plateLogged.Calories && plateAmounts["protein_g"] == plateLogged.Protein && plateAmounts["carbs_g"] == plateLogged.Carbs
    && plateAmounts["fat_g"] == plateLogged.Fat && plateAmounts["fiber_g"] == plateLogged.Fiber, "Plate: SnapshotAmounts khớp cột snapshot entry khi log thật (137 g)");
Check(plateAmounts["vitamin_c_mg"] == NutritionRounding.Round(12.3456m * 1.37m, 4), "Plate: chất ngoài cột entry làm tròn 4 số lẻ như FoodLogEntryNutrient");
var plateSum = NutritionCalculator.SumAmounts([plateAmounts, new Dictionary<string, decimal> { ["energy_kcal"] = 100m, ["protein_g"] = 5m, ["carbs_g"] = 0m, ["fat_g"] = 0m }]);
Check(plateSum["energy_kcal"] == plateAmounts["energy_kcal"] + 100m && plateSum["vitamin_c_mg"] == plateAmounts["vitamin_c_mg"] && !plateSum.ContainsKey("zinc_mg") && NutritionCalculator.SumAmounts([]).Count == 0, "Plate: cộng nhiều dòng (món + quick add), mảng rỗng → rỗng, chất không ai có thì vắng mặt");
var plateValidator = new GetPlateNutrientsQueryValidator();
PlateLine PlateFood() => new(Guid.NewGuid(), null, null, 100m);
Check(plateValidator.Validate(new GetPlateNutrientsQuery(null, null)).IsValid && plateValidator.Validate(new GetPlateNutrientsQuery(null, [])).IsValid, "Plate: items rỗng/null hợp lệ");
Check(plateValidator.Validate(new GetPlateNutrientsQuery(null, Enumerable.Range(0, 50).Select(_ => PlateFood()).ToList())).IsValid && !plateValidator.Validate(new GetPlateNutrientsQuery(null, Enumerable.Range(0, 51).Select(_ => PlateFood()).ToList())).IsValid, "Plate: tối đa 50 dòng");
Check(!plateValidator.Validate(new GetPlateNutrientsQuery(null, [new PlateLine(null, Guid.NewGuid(), Guid.NewGuid(), 1m)])).IsValid
    && !plateValidator.Validate(new GetPlateNutrientsQuery(null, [new PlateLine(Guid.NewGuid(), null, null, 0m)])).IsValid
    && !plateValidator.Validate(new GetPlateNutrientsQuery(null, [new PlateLine(null, null, null, 1m, "x", -5m)])).IsValid
    && plateValidator.Validate(new GetPlateNutrientsQuery(null, [new PlateLine(null, null, null, 1m, "Ước lượng", 300m, 20m, 30m, 10m)])).IsValid, "Plate: recipe có servingId / quantity 0 / macro âm bị từ chối, dòng ước lượng hợp lệ");

// Food icon: bộ khoá, resolver rule-based, validator, icon hiệu lực của user
Check(FoodIconKeys.All.Count == 529 && FoodIconKeys.IsValid("apple") && FoodIconKeys.IsValid("brown_rice") && FoodIconKeys.IsValid("oatmeal_bowl") && FoodIconKeys.IsValid("pizza_slice") && FoodIconKeys.IsValid("tofu") && FoodIconKeys.IsValid("bread_loaf")
    && !FoodIconKeys.IsValid("Apple") && !FoodIconKeys.IsValid("bread") && !FoodIconKeys.IsValid(null) && !FoodIconKeys.IsValid(""), "Icon: 529 khoá hợp lệ (300 cũ giữ nguyên + 229 mới), phân biệt hoa thường, không có khoá 'bread' trần");
Check(FoodIconKeys.NotAutoAssigned.Count == 12 && FoodIconKeys.NotAutoAssigned.All(FoodIconKeys.IsValid) && !IconKeyResolver.AllKeys.Any(FoodIconKeys.NotAutoAssigned.Contains)
    && new[] { "ketchup_bottle", "mustard_bottle", "cheese_crackers", "mixed_nuts", "roasted_chickpeas", "ice_cream_sandwich", "tofu", "seitan", "plant_burger", "bouillon_cube", "tomato_paste", "tempeh" }.All(FoodIconKeys.NotAutoAssigned.Contains),
    "Icon: 12 khoá hình sai vẫn hợp lệ (user chọn tay được) nhưng resolver không bao giờ tự gán");
Check(new[] { "avocado", "pesto", "sunflower_oil", "banh_mi", "pho", "xoi", "mustard_bottle" }.All(FoodIconKeys.IsValid) && FoodIconKeys.All.All(k => k.Length <= FoodIconKeys.MaxLength && k == k.ToLowerInvariant()), "Icon: khoá cũ và mới đều có mặt, chữ thường, ≤ 64 ký tự");
var badIconKeys = IconKeyResolver.AllKeys.Where(k => !FoodIconKeys.IsValid(k)).ToList();
Check(badIconKeys.Count == 0, $"Icon: mọi khoá bảng từ khoá trả ra đều có trong bộ icon ({string.Join(",", badIconKeys)})");
(string Name, FoodCategory? Cat, string? Expect)[] iconCases =
[
    ("Apple, Fresh, With Skin", null, "apple"), ("Táo", FoodCategory.Fruits, "apple"), ("Gà", null, "chicken"), ("Cơm trắng", FoodCategory.Grains, "rice"),
    ("Trứng gà luộc", FoodCategory.Protein, "boiled_egg"), ("Cá basa", FoodCategory.Protein, "catfish"), ("Bò xào trứng", FoodCategory.Protein, "beef"),
    ("Thịt bò nạc nấu chín", FoodCategory.Protein, "beef"), ("Bơ", FoodCategory.Fruits, "avocado"), ("Bơ", FoodCategory.Fats, "butter"), ("Bơ đậu phộng", FoodCategory.Fats, "peanut_butter"),
    ("bo xao trung", null, "eggs"), ("Trung ga luoc", null, "boiled_egg"), ("Ngô luộc", null, "corn"), ("Ngò rí", null, "cilantro"), ("Hành nhỏ", null, "onion"),
    ("Beef, ground, 80% lean meat / 20% fat, raw", null, "ground_beef"), ("Oil, olive, salad or cooking", null, "olive_oil"), ("Egg, white, raw", null, "egg_white"),
    ("Eggs, Grade A, Large, egg whole", null, "eggs"), ("Eggplant, raw", null, "eggplant"), ("Rice noodles", null, "rice_noodles"), ("Cheese, cheddar", null, "cheddar"),
    ("Water spinach", null, "spinach"), ("Peanut butter, smooth", null, "peanut_butter"), ("Hamburger", null, "hamburger"), ("Phở bò", FoodCategory.FastFood, "pho"),
    ("Bún bò Huế", FoodCategory.FastFood, "bun_bo_hue"), ("Nước mắm", null, "fish_sauce"), ("Nước tương", null, "soy_sauce"), ("Sữa chua không đường", FoodCategory.Dairy, "yogurt"),
    ("Cơm gạo lứt", FoodCategory.Grains, "brown_rice"), ("Khoai tây chiên", FoodCategory.FastFood, "french_fries"), ("Cà phê sữa đá", FoodCategory.Beverages, "vietnamese_coffee_milk"),
    ("Bánh mì sandwich", FoodCategory.Grains, "white_bread_slice"), ("Bánh mì thịt", FoodCategory.FastFood, "banh_mi"), ("Đậu hũ", FoodCategory.Protein, null), ("Yến mạch khô", FoodCategory.Grains, "oatmeal_bowl"),
    ("Nước ngọt có gas", FoodCategory.Beverages, "soda_can"), ("Xyz không rõ", FoodCategory.Other, null), ("", FoodCategory.Fruits, null),
    // Món từng thiếu icon (bộ 300 khoá) và món có icon riêng ở bộ 529 khoá: tên tiếng Anh sau reseed
    ("Oats, dry", FoodCategory.Grains, "oatmeal_bowl"), ("Pizza", FoodCategory.FastFood, "pizza_slice"), ("Tofu", FoodCategory.Protein, null), ("Sandwich bread", FoodCategory.Grains, "white_bread_slice"),
    ("Bread roll, plain", FoodCategory.Grains, "bread_roll"), ("French fries", FoodCategory.FastFood, "french_fries"), ("Cola", FoodCategory.Beverages, "cola_bottle"), ("Whey protein powder", FoodCategory.Supplements, "whey_scoop"),
    ("Protein powder", FoodCategory.Supplements, "protein_powder"), ("Fresh spring roll", FoodCategory.FastFood, "goi_cuon"), ("Spring rolls, fried", null, "spring_rolls"), ("Baking Soda", FoodCategory.Spices, "baking_soda"),
    ("Baking Powder", FoodCategory.Spices, "baking_powder"), ("Balsamic Vinegar", FoodCategory.Condiments, "vinegar"), ("White Vinegar", FoodCategory.Condiments, "vinegar"), ("Curry Powder", FoodCategory.Spices, "curry_powder"),
    ("Nutmeg, Ground", FoodCategory.Spices, "nutmeg"), ("Lard", FoodCategory.Fats, "shortening"), ("Vegetable Shortening", FoodCategory.Fats, "shortening"), ("Stevia Sweetener", FoodCategory.Condiments, "stevia"),
    ("Sucralose Sweetener", FoodCategory.Condiments, "sucralose"), ("Ranch Dressing", FoodCategory.Condiments, "ranch_dressing"), ("Seasoning Granules", FoodCategory.Spices, "salt"), ("Tomato paste", FoodCategory.Condiments, "tomato_sauce"), ("Cheese crackers", FoodCategory.Snacks, "cracker"), ("Mixed nuts", FoodCategory.Snacks, null),
    ("Roasted chickpeas", FoodCategory.Snacks, "chickpeas"), ("Ice cream sandwich", FoodCategory.Snacks, "ice_cream_scoop"), ("Tempeh", FoodCategory.Protein, null), ("Seitan", FoodCategory.Protein, null), ("Plant burger", FoodCategory.Protein, "hamburger"), ("Sugarcane Syrup", FoodCategory.Condiments, "syrup_bottle"),
    ("Maple Syrup", FoodCategory.Condiments, "maple_syrup"), ("Whole wheat bread", FoodCategory.Grains, "whole_wheat_bread"), ("Rye bread", FoodCategory.Grains, "rye_bread"), ("Bagel", FoodCategory.Grains, "bagel"),
    ("Flatbread", FoodCategory.Grains, "flatbread"), ("Toast", FoodCategory.Grains, "toast"), ("Bread loaf", FoodCategory.Grains, "bread_loaf"), ("Bread", FoodCategory.Grains, "bread_loaf"),
    ("Egg noodles", FoodCategory.Grains, "ramen_noodles"), ("Fried noodles", FoodCategory.FastFood, "fried_noodles"), ("Rice vermicelli, fresh", FoodCategory.Grains, "rice_noodles"), ("Pho noodles, rice noodles", FoodCategory.Grains, "rice_noodles"),
    ("Soda", FoodCategory.Beverages, "soda_can"), ("Bubble tea", FoodCategory.Beverages, "bubble_tea"), ("Iced milk coffee", FoodCategory.Beverages, "vietnamese_coffee_milk"), ("Black coffee", FoodCategory.Beverages, "coffee"),
    ("Orange juice", FoodCategory.Beverages, "orange_juice"), ("Coconut water", FoodCategory.Beverages, "coconut_water"), ("Dark chocolate", FoodCategory.Snacks, "chocolate_bar"), ("Potato chips", FoodCategory.Snacks, "potato_chips"),
    ("Broken rice with grilled pork chop", FoodCategory.FastFood, "com_tam"), ("Banh mi, pork", FoodCategory.FastFood, "banh_mi"), ("Bun bo Hue, spicy beef noodle soup", FoodCategory.FastFood, "bun_bo_hue"), ("Sticky rice, cooked", FoodCategory.Grains, "xoi"),
    ("Xôi", FoodCategory.Grains, "xoi"), ("Cháo", FoodCategory.Grains, "congee"), ("Mì xào", FoodCategory.FastFood, "fried_noodles"), ("Burger", null, "hamburger"), ("Oatmeal", null, "oatmeal_bowl"),
    ("Fish oil", FoodCategory.Supplements, "fish_oil"), ("Oil, olive, salad or cooking", null, "olive_oil"), ("Beef pho", FoodCategory.FastFood, "pho"), ("Beef, lean, cooked", FoodCategory.Protein, "beef"),
    ("Mồng tơi", FoodCategory.Vegetables, null), ("Quả lạ chưa biết", FoodCategory.Fruits, null), ("Sữa gì đó", FoodCategory.Dairy, "milk"),
];
var iconWrong = iconCases.Where(c => IconKeyResolver.Resolve(c.Name, c.Cat) != c.Expect).Select(c => $"{c.Name}→{IconKeyResolver.Resolve(c.Name, c.Cat) ?? "null"} (cần {c.Expect ?? "null"})").ToList();
Check(iconWrong.Count == 0, $"Icon: resolver đúng {iconCases.Length} ca EN/VN có dấu/không dấu, cụm dài, bò/bơ, ngô/ngò, null; sai: {string.Join("; ", iconWrong)}");
Check(IconKeyResolver.Resolve("Cherry tomatoes") == "cherry_tomato" && IconKeyResolver.Resolve("Soy sauce") == "soy_sauce" && IconKeyResolver.Resolve("Almond flour") == "almond_flour", "Icon: cụm dài thắng cụm ngắn (cherry tomato › tomato, soy sauce, almond flour)");
// Icon (MacroFactor-style): bổ ngữ "with/without salt" không thành icon muối; loại món ở đoạn đầu thắng nguyên liệu; fallback theo nhóm USDA, trái cây không có icon chung
(string Name, FoodCategory? Cat, string? Group, string? Expect)[] iconRules =
[
    ("Squash, summer, all varieties, boiled, drained, without salt", FoodCategory.Vegetables, "Vegetables and Vegetable Products", "yellow_squash"),
    ("Beans, snap, green, canned, regular pack, drained solids, with salt added", FoodCategory.Vegetables, null, "green_beans"),
    ("Salt, table", FoodCategory.Other, null, "salt"), ("Rice, white, long-grain, cooked, no salt added", FoodCategory.Grains, null, "rice"),
    ("Apple pie", FoodCategory.Fruits, null, "pie_slice"), ("Apple Strudel", FoodCategory.Fruits, null, "pie_slice"), ("Pie, apple, commercially prepared", FoodCategory.Fruits, null, "pie_slice"),
    ("Apple juice", FoodCategory.Beverages, null, "apple_juice"), ("Fruit juice drink", FoodCategory.Beverages, null, "juice_box"), ("Grape juice, canned", FoodCategory.Beverages, null, "juice_box"),
    ("Oatmeal", FoodCategory.Grains, null, "oatmeal_bowl"), ("Cereals, ready-to-eat", FoodCategory.Grains, null, "cereal_bowl"), ("Oat flour", FoodCategory.Grains, null, "flour"),
    ("Chicken noodle soup", FoodCategory.Other, null, "soup_bowl"), ("Potato salad with egg", FoodCategory.Other, null, "salad_bowl"), ("Salad dressing, ranch", FoodCategory.Fats, null, "salad_dressing"),
    ("Cookies, oatmeal, soft", FoodCategory.Snacks, null, "cookie"), ("Peanut butter cookie", FoodCategory.Snacks, null, "cookie"), ("Muffins, English, wheat", FoodCategory.Grains, null, "english_muffin"),
    ("Hot dog sandwich, on white bread", FoodCategory.FastFood, null, "sandwich"), ("Ice cream sandwich", FoodCategory.Snacks, null, "ice_cream_scoop"),
    ("Fish, pollock, raw", FoodCategory.Protein, "Finfish and Shellfish Products", "grilled_fish"), ("Fish, salmon, raw", FoodCategory.Protein, "Finfish and Shellfish Products", "salmon"),
    ("Rhubarb, raw", FoodCategory.Fruits, "Fruits and Fruit Juices", null), ("Tangerines, canned, juice pack", FoodCategory.Fruits, "Fruits and Fruit Juices", null),
    ("Quail, cooked, total edible", FoodCategory.Protein, "Poultry Products", "chicken"), ("Game meat, muskrat, raw", FoodCategory.Protein, "Lamb, Veal, and Game Products", null),
    ("Nuts, brazilnuts, raw", FoodCategory.Snacks, "Nut and Seed Products", null), ("Gravy, au jus, canned", FoodCategory.Other, "Soups, Sauces, and Gravies", "soup_bowl"),
    ("Squash, summer, zucchini, includes skin, raw", FoodCategory.Vegetables, "Vegetables and Vegetable Products", "zucchini"), ("Squash, winter, all varieties, cooked", FoodCategory.Vegetables, "Vegetables and Vegetable Products", "butternut_squash"),
    ("Squash, winter, acorn, raw", FoodCategory.Vegetables, null, "acorn_squash"), ("Peas, green, cooked, boiled", FoodCategory.Vegetables, "Vegetables and Vegetable Products", "snap_peas"), ("Watercress, raw", FoodCategory.Vegetables, null, "arugula"),
    ("Chard, swiss, raw", FoodCategory.Vegetables, null, "kale"), ("Beet greens, cooked", FoodCategory.Vegetables, null, "beet"), ("Taro, cooked", FoodCategory.Vegetables, null, "cassava"), ("Seaweed, kelp, raw", FoodCategory.Vegetables, null, "seaweed_salad_veg"),
    ("Okra", FoodCategory.Vegetables, "Vegetables and Vegetable Products", "okra"), ("Gourd, dishcloth, raw", FoodCategory.Vegetables, "Vegetables and Vegetable Products", null), ("Tofu, raw", FoodCategory.Protein, "Legumes and Legume Products", null),
    ("Tea, instant", FoodCategory.Beverages, "Beverages", "tea"), ("Steak sauce", FoodCategory.Other, "Soups, Sauces, and Gravies", "steak_beef"),
];
var ruleWrong = iconRules.Where(c => IconKeyResolver.Resolve(c.Name, c.Cat, c.Group) != c.Expect).Select(c => $"{c.Name}→{IconKeyResolver.Resolve(c.Name, c.Cat, c.Group) ?? "null"} (cần {c.Expect ?? "null"})").ToList();
Check(ruleWrong.Count == 0, $"Icon MF: bổ ngữ muối/drained bị bỏ, loại món ở đoạn đầu thắng nguyên liệu, fallback nhóm USDA (cá/rau/súp), trái cây + thịt rừng + hạt = null{(ruleWrong.Count == 0 ? "" : "; sai: " + string.Join("; ", ruleWrong))}");
Check(IconKeyResolver.AllKeys.All(FoodIconKeys.IsValid) && !IconKeyResolver.AllKeys.Any(FoodIconKeys.NotAutoAssigned.Contains), "Icon: mọi khoá resolver trả ra (kể cả fallback nhóm) đều hợp lệ và không thuộc nhóm hình sai");
// Habits (Weigh-In): streak ngày cân liên tiếp
var weighToday = new DateOnly(2026, 10, 20);
DateOnly[] Days(params int[] back) => back.Select(b => weighToday.AddDays(-b)).ToArray();
Check(WeighInStreak.Compute([], weighToday) is { Current: 0, Longest: 0, IncludesToday: false }, "Streak: chưa cân lần nào → 0");
Check(WeighInStreak.Compute(Days(0, 1, 2, 4, 5), weighToday) is { Current: 3, Longest: 3, IncludesToday: true }, "Streak: cân hôm nay + 2 ngày trước → hiện tại 3 (đứt ở ngày thứ 3 trước), dài nhất 3");
Check(WeighInStreak.Compute(Days(1, 2, 3), weighToday) is { Current: 3, Longest: 3, IncludesToday: false }, "Streak: hôm nay chưa cân thì tính đến hôm qua (chưa đứt), IncludesToday = false");
Check(WeighInStreak.Compute(Days(2, 3, 4, 10, 11, 12, 13, 14), weighToday) is { Current: 0, Longest: 5, IncludesToday: false }, "Streak: bỏ cả hôm qua → chuỗi hiện tại đứt (0), dài nhất vẫn nhớ (5)");
Check(WeighInStreak.Compute(Days(0, 0, 1, 1), weighToday).Current == 2 && WeighInStreak.Compute(Days(0).Append(weighToday.AddDays(3)), weighToday) is { Current: 1, Longest: 1 }, "Streak: ngày trùng chỉ tính một lần; ngày tương lai bị bỏ qua");
var vnIcons = CanonicalCatalogData.VietnameseFoods.Count(v => IconKeyResolver.Resolve(v.Vi, v.Cat) is not null);
Check(vnIcons >= CanonicalCatalogData.VietnameseFoods.Length * 85 / 100, $"Icon: ≥ 85 % món Việt seed gán được icon ({vnIcons}/{CanonicalCatalogData.VietnameseFoods.Length})");

var iconValidator = new SetFoodIconCommandValidator();
Check(iconValidator.Validate(new SetFoodIconCommand(Guid.NewGuid(), "apple")).IsValid && iconValidator.Validate(new SetFoodIconCommand(Guid.NewGuid(), null)).IsValid
    && !iconValidator.Validate(new SetFoodIconCommand(Guid.NewGuid(), "not_an_icon")).IsValid && !iconValidator.Validate(new SetFoodIconCommand(Guid.NewGuid(), new string('a', 65))).IsValid, "Icon: PUT /foods/{id}/icon, khoá lạ → 400, null hợp lệ");
var customIconFood = new CreateCustomFoodCommand("Món", null, FoodCategory.Other, null, new FoodNutrition(100, 1, 1, 1), null, null, "nope");
var customIconValidator = new CreateCustomFoodCommandValidator();
Check(!customIconValidator.Validate(customIconFood).IsValid && customIconValidator.Validate(customIconFood with { IconKey = "apple" }).IsValid && customIconValidator.Validate(customIconFood with { IconKey = null }).IsValid, "Icon: custom food nhận iconKey tuỳ chọn, khoá lạ bị từ chối");
var iconFoodId = Guid.NewGuid();
var iconOverrides = new Dictionary<Guid, string> { [iconFoodId] = "banana" };
Check(FoodIconSupport.Effective(iconFoodId, "apple", iconOverrides) == "banana" && FoodIconSupport.Effective(Guid.NewGuid(), "apple", iconOverrides) == "apple"
    && FoodIconSupport.Effective(null, "apple", iconOverrides) == "apple" && FoodIconSupport.Effective(iconFoodId, null, iconOverrides) == "banana" && FoodIconSupport.Effective(null, null, iconOverrides) is null, "Icon: icon riêng của user thắng icon của món, không có thì lấy icon của món");

// Catalog gia vị / dầu mỡ / đường / nước chấm
var cond = CanonicalCatalogData.Condiments;
var newCanonicalGroups = cond.GroupBy(c => c.Canonical).Where(g => !CanonicalCatalogData.ExistingCanonicalKeys.ContainsKey(g.Key)).ToList();
Check(cond.Select(c => c.Key).Distinct().Count() == cond.Length && cond.Length >= 80, $"Gia vị: key không trùng ({cond.Length} món, {newCanonicalGroups.Count} canonical mới, {cond.GroupBy(c => c.Canonical).Count() - newCanonicalGroups.Count} canonical cũ nhận biến thể)");
var salts = cond.Where(c => c.Canonical == "Salt").ToList();
Check(salts.Count >= 14 && salts.Count(c => c.Rep) == 1 && salts.Single(c => c.Rep).Key == "cond-salt-table" && salts.Single(c => c.Rep).Name == "Salt"
    && new[] { "Kosher Salt", "Sea Salt Non-Iodized", "Himalayan Pink Salt", "Iodized Table Salt", "Fine Sea Salt", "Flaky Sea Salt", "Garlic Salt", "Lite Salt (Salt Substitute)" }.All(n => salts.Any(s => s.Name == n))
    && CanonicalCatalogData.ExistingCanonicalKeys["Salt"] == "vn-salt", $"Gia vị: {salts.Count} loại muối là món riêng cùng canonical Salt, đại diện là muối ăn (Salt)");
Check(cond.GroupBy(c => c.Canonical).All(g => g.Count(c => c.Rep) <= 1) && newCanonicalGroups.All(g => g.Any(c => c.Rep) || g.Count() >= 1), "Gia vị: mỗi canonical có tối đa một dòng đại diện tường minh");
var kosher = CanonicalCatalogData.ParseServings(cond.Single(c => c.Key == "cond-salt-kosher").Servings);
Check(kosher.Any(s => s.ServingName == "1 tsp (Diamond Crystal)" && s.WeightInGrams == 2.8m) && kosher.Any(s => s.ServingName == "1 tsp (Morton)" && s.WeightInGrams == 4.8m), "Gia vị: Kosher Salt có khẩu phần theo Diamond Crystal và Morton, ghi rõ trong tên");
var servingProblems = cond.Select(c => (c.Key, S: CanonicalCatalogData.ParseServings(c.Servings)))
    .Where(x => x.S.Count == 0 || x.S.Any(s => s.WeightInGrams <= 0) || x.S.Count(s => s.IsDefault) != 1 || !x.S.Any(s => s.ServingName.Contains("tsp") || s.ServingName.Contains("tbsp"))).Select(x => x.Key).ToList();
Check(servingProblems.Count == 0, $"Gia vị: mọi món có khẩu phần theo muỗng, gram > 0, đúng một mặc định ({string.Join(",", servingProblems)})");
Check(cond.Where(c => c.Key is "cond-salt-table" or "cond-salt-sea" or "cond-salt-kosher" or "cond-baking-soda" or "cond-msg" or "cond-salt-lite" or "cond-salt-coarse")
        .All(c => c.Kcal == 0 && c.P == 0 && c.C == 0 && c.F == 0 && c.SodiumMg > 0)
    && cond.Single(c => c.Key == "cond-salt-table").SodiumMg == 38800m && cond.Single(c => c.Key == "cond-baking-soda").SodiumMg == 27360m, "Gia vị: kcal = 0 của muối, baking soda, bột ngọt được giữ, kèm sodium đã biết");
Check(cond.Where(c => c.Cat is FoodCategory.Spices && c.SodiumMg is null).All(c => c.Key is not "cond-salt-table") && cond.Where(c => c.Key is "cond-salt-himalayan" or "cond-salt-black" or "cond-salt-garlic" or "cond-salt-seasoned").All(c => c.SodiumMg is null), "Gia vị: sodium chưa biết để Unknown (null), không để 0");
var energyBad = cond.Where(c => c.Cat is FoodCategory.Fats or FoodCategory.Condiments && c.Key is not ("cond-mirin" or "cond-cooking-wine"))
    .Where(c => !NutritionCalculator.ValidateEnergy(c.Kcal, c.P, c.C, c.F).IsConsistent || c.P + c.C + c.F > 105 || c.Kcal > 900).Select(c => c.Key).ToList();
Check(energyBad.Count == 0, $"Gia vị: kcal khớp 4P+4C+9F (dầu, đường, nước chấm), tổng macro ≤ 105 g/100 g ({string.Join(",", energyBad)})");
var condKeys = cond.Select(c => c.Key).ToHashSet();
Check(CanonicalCatalogData.CondimentVerifiedKeys.All(condKeys.Contains) && CanonicalCatalogData.CondimentUncertainKeys.All(condKeys.Contains) && !CanonicalCatalogData.CondimentVerifiedKeys.Overlaps(CanonicalCatalogData.CondimentUncertainKeys), $"Gia vị: danh sách Verified ({CanonicalCatalogData.CondimentVerifiedKeys.Count}) và Uncertain ({CanonicalCatalogData.CondimentUncertainKeys.Length}) hợp lệ, không chồng nhau");
Check(cond.All(c => IconKeyResolver.Resolve(c.Name, c.Cat) is null || FoodIconKeys.IsValid(IconKeyResolver.Resolve(c.Name, c.Cat))) && cond.Count(c => IconKeyResolver.Resolve(c.Name, c.Cat) is not null) >= cond.Length * 85 / 100,
    $"Gia vị: {cond.Count(c => IconKeyResolver.Resolve(c.Name, c.Cat) is not null)}/{cond.Length} món gán được icon");

// Tìm kiếm: canonical gia vị và biến thể đứng trước món chỉ chứa từ khoá trong tên
var entryAliases = CanonicalCatalogData.Entries(seedNames).ToDictionary(e => e.En, e => e.Aliases.Select(FoodNameNormalizer.Normalize).Where(a => a.Length > 0).Distinct().ToList());
void RankCheck(string query, string canonical, string[] decoys)
{
    var variants = cond.Where(c => c.Canonical == canonical).Select(c => c.Name).ToList();
    var items = variants.Select(n => (Name: n, Aliases: entryAliases[canonical])).Concat(decoys.Select(d => (Name: d, Aliases: new List<string>()))).ToList();
    var ranked = FoodMatchRanker.Rank(items, FoodNameNormalizer.Normalize(query), i => i.Name, i => i.Aliases, _ => 0).Select(i => i.Name).ToList();
    Check(ranked.Take(variants.Count).All(variants.Contains), $"Tìm kiếm: '{query}' đưa {canonical} và {variants.Count} biến thể lên trước {string.Join(", ", decoys)}");
}
RankCheck("salt", "Salt", ["Saltine Crackers", "Salt & Vinegar Chips", "Beans, Cooked Without Salt", "Pretzels, Salted"]);
RankCheck("muối", "Salt", ["Muối đậu phộng rang", "Cá khô ướp muối"]);
RankCheck("oil", "Oil", ["Oil-Roasted Peanuts", "Tuna, Canned in Oil"]);
RankCheck("sugar", "Sugar", ["Sugar Snap Peas", "Cereal, Frosted Sugar Flakes"]);
RankCheck("butter", "Butter", ["Butterfinger Candy Bar", "Peanut Butter Cookies", "Buttermilk Pancakes"]);
RankCheck("dau mè", "Sesame Oil", ["Kẹo mè", "Bánh mè"]);
Check(entryAliases["Salt"].Contains("muoi") && entryAliases["Salt"].Contains("kosher salt") && entryAliases["Salt"].Contains("sea salt non iodized") && entryAliases["Butter"].Contains("butter"), "Tìm kiếm: alias 'muối', 'kosher salt', 'sea salt' thuộc canonical Salt, 'butter' thuộc Butter");

// Strategy: số ngày tới check-in kế tiếp (cùng quy tắc với preview check-in)
var ciStart = new DateOnly(2026, 10, 1); // thứ Năm
var (ciNext1, _) = CheckInCalculator.Next(new DateOnly(2026, 10, 4), DayOfWeek.Monday, ciStart, null);
var (ciNext2, _) = CheckInCalculator.Next(new DateOnly(2026, 10, 8), DayOfWeek.Monday, ciStart, new DateOnly(2026, 10, 5));
Check(ciNext1 == new DateOnly(2026, 10, 5) && CheckInCalculator.DaysUntil(new DateOnly(2026, 10, 4), ciNext1) == 1 && CheckInCalculator.DaysUntil(new DateOnly(2026, 10, 5), ciNext1) == 0, "Check-in: lần đầu là thứ Hai đầu tiên sau ngày bắt đầu, còn 1 ngày rồi 0 ngày");
Check(CheckInCalculator.DaysUntil(new DateOnly(2026, 10, 10), ciNext1) == 0 && ciNext2 == new DateOnly(2026, 10, 12) && CheckInCalculator.DaysUntil(new DateOnly(2026, 10, 8), ciNext2) == 4, "Check-in: quá hạn tính 0 ngày; sau check-in cuối tính tới thứ Hai kế tiếp (4 ngày)");

// Refresh token: access token do JwtTokenService phát ra phải đọc lại được claim "sub" qua GetPrincipalFromExpiredToken; hạn refresh dùng chung một giá trị
var jwtOpts = Microsoft.Extensions.Options.Options.Create(new JwtOptions { SecretKey = new string('k', 40), Issuer = "issuer", Audience = "aud", AccessTokenExpirationMinutes = 15, RefreshTokenExpirationDays = 14 });
var jwtService = new JwtTokenService(jwtOpts);
var jwtUserId = Guid.NewGuid();
var jwtAccess = jwtService.GenerateAccessToken(new UserCredentials { Id = jwtUserId, Email = "a@b.c", FullName = "A", Role = "User" });
var jwtPrincipal = jwtService.GetPrincipalFromExpiredToken(jwtAccess.AccessToken);
Check(jwtPrincipal.FindFirst("sub")?.Value == jwtUserId.ToString(), "Auth: GetPrincipalFromExpiredToken trả đúng claim 'sub' (refresh token đọc được user id)");
var jwtExpiry = jwtService.GetRefreshTokenExpiry();
Check(Math.Abs((jwtExpiry - DateTimeOffset.UtcNow.AddDays(14)).TotalSeconds) < 5, "Auth: hạn refresh token lấy từ JwtOptions.RefreshTokenExpirationDays (14 ngày), không hard-code 30");

// --- AI scan kiểu MacroFactor: quantity/unit/state, biến thể theo cách chế biến, khẩu phần đếm được ("3 large") ---
Check(FoodPreparationState.Detect("Egg, hard-boiled") == "boiled" && FoodPreparationState.Detect("Egg, whole, raw") == "raw" && FoodPreparationState.Detect("Egg, fried") == "fried"
    && FoodPreparationState.Detect("Stir-fried beef") == "stir_fried" && FoodPreparationState.Detect("Egg, scrambled") == "scrambled" && FoodPreparationState.Detect("trứng chiên") == "fried"
    && FoodPreparationState.Detect("Trứng luộc") == "boiled" && FoodPreparationState.Detect("Egg, poached") == "poached" && FoodPreparationState.Detect("Egg") == "none"
    && FoodPreparationState.Detect(null) == "none" && FoodPreparationState.Detect("Olive Oil") == "none", "State: suy từ tên món (EN/VI); không có từ chế biến → none");
Check(FoodPreparationState.Normalize("Stir-Fried") == "stir_fried" && FoodPreparationState.Normalize("none") == "none" && FoodPreparationState.Normalize("poached") == "none" && FoodPreparationState.Normalize(null) == "none"
    && FoodPreparationState.IsValid(null) && FoodPreparationState.IsValid("raw") && FoodPreparationState.IsValid("Stir-Fried") && !FoodPreparationState.IsValid("burnt"), "State: Normalize/IsValid theo bộ AI (poached không phải giá trị AI gửi)");
Check(AiIngredientResolution.NormalizeUnit("Extra_Large") == "extra large" && AiIngredientResolution.NormalizeUnit("tbsp") == "tbsp" && AiIngredientResolution.NormalizeUnit("gallon") is null && AiIngredientResolution.NormalizeUnit(null) is null
    && AiIngredientResolution.CacheKey("egg", "none") == "egg" && AiIngredientResolution.CacheKey("egg", "fried") == "egg|fried"
    && AiIngredientResolution.ServingLabel("1 large") == "large" && AiIngredientResolution.ServingLabel("1 extra large") == "extra large" && AiIngredientResolution.ServingLabel("100 g") == "100 g" && AiIngredientResolution.ServingLabel("1/2 cup") == "1/2 cup",
    "Resolve: NormalizeUnit, khoá đệm thêm state, unitLabel bỏ tiền tố '1 '");

var eggRaw = CanonicalCatalogData.CountableFoods.Single(c => c.Key == "cond-egg-raw");
var eggServings = CanonicalCatalogData.ParseServings(eggRaw.Servings).Select(s => (s.Id, s.ServingName, s.WeightInGrams)).ToList();
Check(eggRaw.Name == "Egg, whole, raw" && eggRaw.Canonical == "Egg" && eggRaw.Rep && eggRaw.Kcal == 143 && eggRaw.P == 12.6m && eggRaw.F == 9.5m && eggRaw.C == 0.7m
    && CanonicalCatalogData.ExistingCanonicalKeys["Egg"] == "egg-boiled" && !CanonicalCatalogData.CondimentVerifiedKeys.Contains(eggRaw.Key)
    && eggServings.Single(s => s.ServingName == "1 large").WeightInGrams == 50 && eggServings.First().ServingName == "1 large"
    && new[] { ("1 small", 38m), ("1 medium", 44m), ("1 extra large", 56m), ("1 jumbo", 63m) }.All(x => eggServings.Any(s => s.ServingName == x.Item1 && s.WeightInGrams == x.Item2)),
    "Catalog: Egg đại diện 'Egg, whole, raw' (143 kcal, Unverified), large 50 mặc định, small 38 / medium 44 / extra large 56 / jumbo 63");
var eggKinds = CanonicalCatalogData.CountableFoods.Where(c => c.Canonical == "Egg").ToList();
Check(eggKinds.Count(c => c.Rep) == 1 && new[] { "raw", "fried", "scrambled", "poached" }.All(st => eggKinds.Any(c => FoodPreparationState.Detect(c.Name) == st))
    && FoodPreparationState.Detect(FoodEnglish.SeedNames["egg-boiled"]) == "boiled" && eggKinds.Single(c => c.Key == "cond-egg-fried").Aliases.Contains("trứng chiên"),
    "Catalog: raw là đại diện duy nhất; hard-boiled (seed cũ), fried, scrambled, poached là biến thể có state suy được từ tên + alias Việt");

var egg3 = AiIngredientResolution.ChooseByUnit(eggServings, "large", 3, 150);
var egg3Macros = NutritionCalculator.SnapshotMacros(eggRaw.Kcal, eggRaw.P, eggRaw.C, eggRaw.F, null, egg3!.WeightGrams);
Check(egg3 is { Label: "large", Quantity: 3, WeightGrams: 150, Corrected: false } && Math.Abs(egg3Macros.Calories - 214.5m) <= 1m,
    $"Resolve: {{Egg,150 g,3,large}} → serving large × 3 = 150 g ≈ {egg3Macros.Calories} kcal");
Check(AiIngredientResolution.ChooseByUnit(eggServings, "extra large", 1, 56) is { Label: "extra large", WeightGrams: 56 }
    && AiIngredientResolution.MatchServing([(Guid.NewGuid(), "1 extra large", 56m)], "large") is null
    && AiIngredientResolution.ChooseByUnit(eggServings, "cup", 2, 100) is null && AiIngredientResolution.ChooseByUnit(eggServings, "piece", null, 100) is { Label: "piece", Quantity: 2, WeightGrams: 100 }
    && AiIngredientResolution.ChooseByUnit(eggServings, "piece", null, 1000) is null,
    "Resolve: cỡ chỉ khớp đúng tên ('large' không rơi vào 'extra large'), đơn vị không khớp → null (fallback gram), thiếu quantity suy từ gram trong 0.25–4");
var eggOff = AiIngredientResolution.ChooseByUnit(eggServings, "large", 3, 300);
Check(eggOff is { Corrected: true, Quantity: 3, WeightGrams: 150 } && AiIngredientResolution.ChooseByUnit(eggServings, "large", 3, 170) is { Corrected: false },
    "Resolve: gram AI lệch > 40% so với quantity × khẩu phần → tin khẩu phần DB (đánh dấu Corrected để ghi log); lệch ≤ 40% không đánh dấu");
var forGram = AiIngredientResolution.ForGramChoice(eggServings);
var gramChoice = NutritionCalculator.ChooseServing(150, forGram);
Check(forGram.All(s => !s.Name.Contains("large") && !s.Name.Contains("jumbo")) && gramChoice is { Quantity: 3, UnitLabel: "1 piece", WeightGrams: 150 }
    && NutritionCalculator.ChooseServing(150, eggServings).UnitLabel == "1 jumbo" && AiIngredientResolution.ForGramChoice([(Guid.NewGuid(), "1 large", 50m)]).Count == 1,
    "Resolve: request cũ (không unit) bỏ khẩu phần cỡ khi chọn theo gram → 150 g trứng ra '3 piece' chứ không ra '2.38 jumbo'; món chỉ có khẩu phần cỡ vẫn dùng được");

IReadOnlyList<Tuple<string, bool>> eggCandidates = [Tuple.Create("Egg, whole, raw", true), Tuple.Create("Egg, hard-boiled", false), Tuple.Create("Egg, fried", false), Tuple.Create("Egg, scrambled", false), Tuple.Create("Egg, poached", false)];
IReadOnlyList<Tuple<string, bool>> oilCandidates = [Tuple.Create("Cooking oil", true), Tuple.Create("Olive Oil", false), Tuple.Create("Extra Virgin Olive Oil", false)];
(string Name, string Source)? Pick(IReadOnlyList<Tuple<string, bool>> items, string name, string state)
    => AiIngredientResolution.PickFood(items, i => i.Item1, i => i.Item2, [FoodNameNormalizer.Normalize(name)], state) is { } p ? (p.Item.Item1, p.Source) : null;
Check(Pick(eggCandidates, "Egg", "none") == ("Egg, whole, raw", "canonical") && Pick(eggCandidates, "Egg", "raw") == ("Egg, whole, raw", "canonical")
    && Pick(eggCandidates, "Egg", "fried") == ("Egg, fried", "variant") && Pick(eggCandidates, "Egg", "boiled") == ("Egg, hard-boiled", "variant")
    && Pick(eggCandidates, "Egg", "scrambled") == ("Egg, scrambled", "variant") && Pick(eggCandidates, "Egg", "grilled") == ("Egg, whole, raw", "canonical")
    && Pick(eggCandidates, "Egg, poached", "none") == ("Egg, poached", "variant"), "Resolve: state none → đại diện (raw); state fried/boiled/scrambled → biến thể cùng state; state không có biến thể → đại diện");
Check(Pick(oilCandidates, "Olive Oil", "none") == ("Olive Oil", "variant") && Pick(oilCandidates, "Oil", "none") == ("Cooking oil", "canonical")
    && Pick(oilCandidates, "Extra Virgin Olive Oil", "none") == ("Extra Virgin Olive Oil", "variant") && Pick([], "x", "none") is null
    && Pick([Tuple.Create("Brand A", false)], "Egg", "none") is null, "Resolve: tên trùng hẳn một biến thể (Olive Oil) thắng dòng đại diện (Cooking oil); không có món dùng được → null");

// Phân tích: tool JSON có quantity/unit/state tuỳ chọn → làm sạch
var mealJson = System.Text.Json.JsonDocument.Parse("""
    {"confidence":"high","dishes":[{"name":"Egg","ingredients":[
      {"name":"Egg","grams":150,"quantity":3,"unit":"large","state":"none"},
      {"name":"Vegetable Oil","grams":5,"state":"fried"},
      {"name":"Bread","grams":60,"quantity":2,"unit":"gallon"},
      {"name":"Rice","grams":200,"quantity":"2"},
      {"name":"Banana","grams":118,"quantity":1,"unit":"Extra_Large","state":"RAW"},
      {"name":"Apple","grams":182,"quantity":500,"unit":"medium"},
      {"name":"Toast","grams":28}]}]}
    """).RootElement;
var mealIngredients = AiPlateNormalizer.Normalize(AiMealParser.ParseMeal(mealJson)).Dishes.Single().Ingredients.ToDictionary(i => i.Name);
Check(mealIngredients["Egg"] is { Quantity: 3, Unit: "large", State: null, Grams: 150 } && mealIngredients["Vegetable Oil"] is { Quantity: null, Unit: null, State: "fried" }
    && mealIngredients["Bread"] is { Quantity: null, Unit: null } && mealIngredients["Rice"] is { Quantity: null, Unit: null }
    && mealIngredients["Banana"] is { Quantity: 1, Unit: "extra large", State: "raw" } && mealIngredients["Apple"] is { Quantity: null, Unit: null }
    && mealIngredients["Toast"] is { Quantity: null, Unit: null, State: null, Grams: 28 },
    "Phân tích: quantity/unit/state tuỳ chọn được làm sạch (unit lạ, quantity thiếu unit hoặc > 100 bị bỏ; state none → null; request cũ không có 3 trường vẫn như trước)");
var resolveVal = new ResolveAiIngredientQueryValidator();
Check(resolveVal.Validate(new ResolveAiIngredientQuery("Egg", 150, 3, "large", "none")).IsValid && resolveVal.Validate(new ResolveAiIngredientQuery("Egg", 150)).IsValid
    && resolveVal.Validate(new ResolveAiIngredientQuery("Egg", 150, 1, "Extra_Large", "Stir-Fried")).IsValid
    && !resolveVal.Validate(new ResolveAiIngredientQuery("Egg", 150, 0, "large", null)).IsValid && !resolveVal.Validate(new ResolveAiIngredientQuery("Egg", 150, 101, "large", null)).IsValid
    && !resolveVal.Validate(new ResolveAiIngredientQuery("Egg", 150, 3, "gallon", null)).IsValid && !resolveVal.Validate(new ResolveAiIngredientQuery("Egg", 150, 3, "large", "burnt")).IsValid,
    "Resolve: validator nhận quantity/unit/state tuỳ chọn, chặn số lượng ngoài (0,100], unit/state lạ");
var confirmVal = new ConfirmAiScanCommandValidator();
Check(confirmVal.Validate(new ConfirmAiScanCommand(Guid.NewGuid(), [new AiConfirmedIngredient("egg", Guid.NewGuid(), null, "fried")])).IsValid
    && confirmVal.Validate(new ConfirmAiScanCommand(Guid.NewGuid(), [new AiConfirmedIngredient("egg", Guid.NewGuid(), null)])).IsValid
    && !confirmVal.Validate(new ConfirmAiScanCommand(Guid.NewGuid(), [new AiConfirmedIngredient("egg", Guid.NewGuid(), null, "burnt")])).IsValid, "Confirm: nhận state tuỳ chọn, chặn state lạ");

var ruleNames = AiMealPrompt.Build(["Egg", "Salt"]);
Check(ruleNames.IndexOf("Egg; Salt", StringComparison.Ordinal) is >= 0 and var namesAt && ruleNames.IndexOf("Countable foods", StringComparison.Ordinal) > namesAt && ruleNames.IndexOf("State: set state", StringComparison.Ordinal) > namesAt
    && ruleNames.Contains("a large egg is 50 g") && ruleNames.Contains("exactly 1 dish with 1 ingredient") && ruleNames.Contains("Hidden cooking ingredients") && ruleNames.Contains("Do NOT output calories or macros"),
    "Prompt: luật đếm/state/món một thứ nằm SAU danh sách canonical (giữ prompt cache), luật cũ còn nguyên");
var mealSchema = (System.Text.Json.Nodes.JsonNode)typeof(AnthropicMealAnalyzer).GetField("MealSchema", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
var ingSchema = mealSchema["properties"]!["dishes"]!["items"]!["properties"]!["ingredients"]!["items"]!;
HashSet<string> EnumOf(string prop) => ingSchema["properties"]![prop]!["enum"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
Check(EnumOf("unit").SetEquals(AiIngredientResolution.Units) && EnumOf("state").SetEquals(FoodPreparationState.Values) && ingSchema["properties"]!["quantity"]!["type"]!.GetValue<string>() == "number"
    && ingSchema["required"]!.AsArray().Select(n => n!.GetValue<string>()).SequenceEqual(["name", "grams"]), "Tool report_meal: quantity/unit/state tuỳ chọn, enum khớp bộ BE; grams vẫn bắt buộc");

var servingOptions = AiIngredientResolution.ServingOptions(CanonicalCatalogData.ParseServings(eggRaw.Servings).Select(s => (s.Id, s.ServingName, s.WeightInGrams, s.IsDefault)).Append((Guid.NewGuid(), "bad", 0m, false)));
Check(servingOptions.First() is { Label: "large", GramsPerUnit: 50, IsDefault: true } && servingOptions.Count == 7 && servingOptions.All(s => !s.Label.StartsWith("1 ") && s.GramsPerUnit > 0)
    && servingOptions.Skip(1).Select(s => s.GramsPerUnit).SequenceEqual(servingOptions.Skip(1).Select(s => s.GramsPerUnit).Order()) && servingOptions.Any(s => s is { Label: "g", GramsPerUnit: 1 })
    && servingOptions.Select(s => s.Label).Intersect(["small", "medium", "large", "extra large", "jumbo", "piece"]).Count() == 6,
    "Resolve: servings trả cho FE (chip đổi cỡ): nhãn bỏ '1 ', mặc định đứng đầu rồi gram tăng dần, bỏ khẩu phần gram ≤ 0");

var countable = CanonicalCatalogData.CountableFoods;
Check(countable.Select(c => c.Key).Distinct().Count() == countable.Length && !countable.Select(c => c.Key).Intersect(CanonicalCatalogData.Condiments.Select(c => c.Key)).Any()
    && CanonicalCatalogData.CatalogVariants.Length == CanonicalCatalogData.Condiments.Length + countable.Length && CanonicalCatalogData.CountableUncertainKeys.All(k => countable.Any(c => c.Key == k))
    && countable.All(c => { var s = CanonicalCatalogData.ParseServings(c.Servings); return s.Count(x => x.IsDefault) == 1 && s.All(x => x.WeightInGrams > 0) && s.Any(x => x.ServingName != "1 g"); })
    && countable.All(c => NutritionCalculator.ValidateEnergy(c.Kcal, c.P, c.C, c.F).IsConsistent), "Catalog đếm được: key không trùng, mỗi món một khẩu phần mặc định + có đơn vị đếm, kcal khớp 4P+4C+9F");
Check(CanonicalCatalogData.ServingAdditions.All(a => FoodData.VietnameseNames.ContainsKey(a.Key) && CanonicalCatalogData.ParseServings(a.Value).All(s => s.WeightInGrams > 0))
    && CanonicalCatalogData.ServingAdditions["banana"].Contains("1 large=136") && CanonicalCatalogData.ServingAdditions["egg-boiled"].Contains("1 large=50"), "Catalog: khẩu phần đếm bổ sung cho món seed cũ hợp lệ (key tồn tại, gram > 0)");

// --- verify-usda: khớp món catalog với USDA FDC, so macro/chất phụ (hàm thuần; DB + client giả kiểm ở scratch InMemory) ---
UsdaFood Fdc(int id, string desc, params (string Number, string Unit, decimal Amount)[] n)
    => new(id, desc, "SR Legacy", null, [], (n.Length == 0 ? [("208", "kcal", 100m), ("203", "g", 5m)] : n).Select(x => new UsdaNutrientValue(x.Number, x.Unit, x.Amount)).ToList());
Check(UsdaCatalogMatcher.Query("Steamed pork bun (banh bao)") == "Steamed pork bun" && UsdaCatalogMatcher.Query("Egg, hard-boiled") == "Egg hard-boiled"
    && UsdaCatalogMatcher.Tokens("Bananas, raw").SequenceEqual(["banana", "raw"]) && UsdaCatalogMatcher.Tokens("Fish sauce and the (nuoc mam)").SequenceEqual(["fish", "sauce"]),
    "USDA verify: từ khoá tìm bỏ ngoặc/dấu phẩy, token số ít, bỏ từ nối");
var hardBoiled = Fdc(1, "Egg, whole, cooked, hard-boiled");
var rawEgg = Fdc(2, "Egg, whole, raw, fresh");
var friedEgg = Fdc(3, "Egg, whole, cooked, fried");
Check(UsdaCatalogMatcher.Score("Egg, hard-boiled", hardBoiled).Grade == UsdaMatchGrade.Match && UsdaCatalogMatcher.Score("Egg, hard-boiled", rawEgg).Grade != UsdaMatchGrade.Match
    && UsdaCatalogMatcher.Score("Egg, whole, raw", hardBoiled).Grade == UsdaMatchGrade.Weak && UsdaCatalogMatcher.Score("Egg, whole, raw", rawEgg).Grade == UsdaMatchGrade.Match
    && UsdaCatalogMatcher.Pick("Egg, hard-boiled", [rawEgg, friedEgg, hardBoiled])!.Food.FdcId == 1,
    "USDA verify: cách chế biến nằm trong độ phủ (hard-boiled không bị xác minh bằng trứng raw/fried); chọn đúng ứng viên");
Check(UsdaCatalogMatcher.Score("Cinnamon, Ground", Fdc(4, "Beef, ground, raw")).Grade == UsdaMatchGrade.None && UsdaCatalogMatcher.Score("Salt", Fdc(5, "Salt, table")).Grade == UsdaMatchGrade.Match
    && UsdaCatalogMatcher.Score("Olive Oil", Fdc(6, "Oil, olive, salad or cooking")).Grade == UsdaMatchGrade.Match && UsdaCatalogMatcher.Score("Sea Salt Non-Iodized", Fdc(5, "Salt, table")).Grade != UsdaMatchGrade.Match
    && UsdaCatalogMatcher.Pick("Salt", [Fdc(7, "Salt substitute, potassium chloride blend"), Fdc(5, "Salt, table")])!.Food.FdcId == 5
    && UsdaCatalogMatcher.Pick("Salt", []) is null, "USDA verify: từ chung (ground/raw…) không đủ để khớp; muối ăn khớp, 'Sea Salt Non-Iodized' chỉ weak; mô tả ngắn hơn thắng khi hoà điểm");
var usdaMacros = UsdaCatalogMatcher.Macros(Fdc(8, "x", ("208", "kcal", 155m), ("203", "g", 12.58m), ("205", "g", 1.12m), ("204", "g", 10.61m)));
var atwater = UsdaCatalogMatcher.Macros(Fdc(9, "x", ("957", "kcal", 120m), ("203", "g", 5m)));
Check(usdaMacros == new UsdaMacros(155m, 12.58m, 1.12m, 10.61m) && atwater == new UsdaMacros(120m, 5m, null, null) && UsdaCatalogMatcher.Macros(Fdc(10, "x", ("268", "kJ", 500m))).Kcal is null,
    "USDA verify: macro đọc 208/203/205/204, thiếu kcal thì Atwater 957/958, kJ không tính");
var diffEgg = UsdaCatalogMatcher.MacroDiffs(155m, 12.6m, 1.1m, 10.6m, usdaMacros);
var diffBad = UsdaCatalogMatcher.MacroDiffs(76m, 8m, 1.9m, 4.8m, new UsdaMacros(144m, 17.27m, 2.78m, 8.72m));
Check(diffEgg.Count == 0 && diffBad.Select(d => d.Field).SequenceEqual(["kcal", "protein", "carbs", "fat"]) && diffBad[0].Percent == Math.Round(68m / 144m, 4)
    && UsdaCatalogMatcher.MacroDiffs(100m, 0.2m, 0m, 0m, new UsdaMacros(100m, 0.4m, null, 0m)).Count == 0
    && UsdaCatalogMatcher.MacroDiffs(0m, 0m, 0m, 0m, new UsdaMacros(884m, null, null, null)).Select(d => d.Field).SequenceEqual(["kcal"]),
    "USDA verify: chỉ báo macro lệch > 5 % và đủ lớn tuyệt đối (≥ 3 kcal, ≥ 0.3 g); chất USDA không có thì bỏ qua");
var nutrientDiffs = UsdaCatalogMatcher.NutrientDiffs([("sodium_mg", "307", "mg", 38800m), ("vitamin_a_mcg", "320", "mcg", 100m), ("fiber_g", "291", "g", 2m), ("custom", null, "g", 5m)],
    Fdc(11, "x", ("307", "mg", 38758m), ("320", "µg", 150m), ("291", "g", 2.05m)));
Check(nutrientDiffs.Count == 1 && nutrientDiffs[0].Field == "vitamin_a_mcg" && nutrientDiffs[0].Percent == Math.Round(50m / 150m, 4),
    "USDA verify: chất phụ so cùng đơn vị (µg = mcg), lệch ≤ 5 % hoặc không có số USDA thì bỏ qua");
Check(UsdaCatalogMatcher.IsSuspicious(76m, 8m, 1.9m, 4.8m, new UsdaMacros(144m, 17.27m, 2.78m, 8.72m)) && !UsdaCatalogMatcher.IsSuspicious(155m, 12.6m, 1.1m, 10.6m, new UsdaMacros(143m, 12.58m, 0.72m, 9.5m))
    && !UsdaCatalogMatcher.IsSuspicious(50m, 1m, 1m, 1m, new UsdaMacros(null, null, null, null))
    && UsdaCatalogMatcher.IsSuspicious(0m, 0m, 0m, 0m, new UsdaMacros(0m, 0m, 100m, 0m)) && UsdaCatalogMatcher.IsSuspicious(100m, 10m, 10m, 10m, new UsdaMacros(100m, 10m, 10m, 4m))
    && !UsdaCatalogMatcher.IsSuspicious(100m, 1m, 10m, 10m, new UsdaMacros(100m, 1.8m, 10m, 10m)) && UsdaCatalogMatcher.IsSuspicious(130m, 5m, 20m, 5m, new UsdaMacros(100m, 5m, 20m, 5m)),
    "USDA verify: tên khớp mà kcal lệch > 25 % hoặc protein/carbs/fat lệch > 50 % (khi một bên ≥ 2 g) → nghi khớp nhầm (cần xem, không tự ghi); lệch nhỏ ở giá trị < 2 g thì bỏ qua");

// Hồi quy các cặp khớp SAI từ dry-run thật (mô tả ứng viên giả theo đúng kiểu đặt tên của FDC): phải KHÔNG ra Match; ứng viên đúng phải ra Match
UsdaFood Cand(int id, string desc, decimal kcal = 100, string type = "SR Legacy", string? category = null)
    => new(id, desc, type, category, [], [new("208", "kcal", kcal), new("203", "g", 5m), new("204", "g", 5m), new("205", "g", 10m)]);
var wrongPairs = new (string Ours, string Usda)[]
{
    ("Almonds", "Oil, almond"), ("Peanuts", "Oil, peanut"), ("Tofu", "Tofu yogurt"), ("Soy milk", "Milk, imitation, non-soy"), ("Milk, unsweetened", "Beverages, rice milk, unsweetened"),
    ("Potato, boiled", "Sweet potato, cooked, boiled, without skin"), ("Orange juice", "Babyfood, juice, orange"), ("Chicken breast, cooked", "Chicken, breast, tenders, breaded, cooked, microwaved"),
    ("Tapioca Starch", "Bread, gluten-free, white, made with tapioca starch and brown rice flour"), ("Sandwich bread", "Rudi's Organic Bakery, 100% Whole Wheat Bread"),
    ("White rice, cooked", "Rice, glutinous, white, cooked"), ("Sugar", "Sugars, brown"), ("Vegetable Oil", "Oil, palm kernel"), ("Hamburger", "McDONALD'S, Hamburger"),
    ("Pizza", "PIZZA HUT 12\" Pepperoni Pizza"), ("French fries", "DENNY'S, french fries"), ("Dark chocolate", "Candies, SPECIAL DARK Chocolate Bar"), ("Oats, dry", "Cereals, QUAKER, Oatmeal, dry"),
    ("Egg, whole, raw", "Egg, whole, fried"), ("Egg, hard-boiled", "Egg, whole, cooked, fried"), ("Milk, unsweetened", "Milk, nonfat, fluid"), ("Banana", "Bananas, dehydrated, or banana powder"),
};
var wrongStillMatch = wrongPairs.Where(p => UsdaCatalogMatcher.Score(p.Ours, Cand(1, p.Usda)).Grade == UsdaMatchGrade.Match).Select(p => $"{p.Ours} ↔ {p.Usda}").ToList();
Check(wrongStillMatch.Count == 0, $"USDA verify hồi quy: {wrongPairs.Length} cặp khớp sai (oil/juice/yogurt/imitation/rice milk/sweet/babyfood/breaded/glutinous/brown/thương hiệu/trạng thái) không còn ra Match ({string.Join("; ", wrongStillMatch)})");
var rightPairs = new (string Ours, string Usda)[]
{
    ("Almonds", "Nuts, almonds"), ("Tofu", "Tofu, raw, firm, prepared with calcium sulfate"), ("Milk, unsweetened", "Milk, whole, 3.25% milkfat, with added vitamin D"), ("Potato, boiled", "Potatoes, boiled, cooked without skin, flesh, without salt"),
    ("Chicken breast, cooked", "Chicken, broilers or fryers, breast, meat only, cooked, roasted"), ("White rice, cooked", "Rice, white, long-grain, regular, cooked, enriched"), ("Sugar", "Sugars, granulated"),
    ("Orange juice", "Orange juice, raw"), ("Banana", "Bananas, raw"), ("Egg, whole, raw", "Egg, whole, raw, fresh"), ("Egg, hard-boiled", "Egg, whole, cooked, hard-boiled"), ("Salt", "Salt, table"),
    ("Beef pho", "Soup, beef pho"), ("Oats, dry", "Cereals, oats, regular and quick, not fortified, dry"),
};
var rightMissed = rightPairs.Where(p => UsdaCatalogMatcher.Score(p.Ours, Cand(1, p.Usda)).Grade != UsdaMatchGrade.Match).Select(p => $"{p.Ours} ↔ {p.Usda}").ToList();
Check(rightMissed.Count == 0, $"USDA verify hồi quy: {rightPairs.Length} ứng viên ĐÚNG (Almonds raw, Tofu raw firm, Milk whole, Potatoes boiled, Chicken breast roasted, Rice cooked…) vẫn ra Match ({string.Join("; ", rightMissed)})");
Check(UsdaCatalogMatcher.Pick("Almonds", [Cand(1, "Oil, almond"), Cand(2, "Nuts, almond butter, plain"), Cand(3, "Nuts, almonds")])!.Food.FdcId == 3
    && UsdaCatalogMatcher.Pick("White rice, cooked", [Cand(1, "Rice, glutinous, white, cooked"), Cand(2, "Rice, white, long-grain, regular, cooked"), Cand(3, "Rice, white, long-grain, regular, raw, enriched")])!.Food.FdcId == 2
    && UsdaCatalogMatcher.Pick("Milk, unsweetened", [Cand(1, "Beverages, rice milk, unsweetened"), Cand(2, "Milk, nonfat, fluid"), Cand(3, "Milk, whole, 3.25% milkfat, with added vitamin D")])!.Food.FdcId == 3
    && UsdaCatalogMatcher.Pick("Egg, whole, raw", [Cand(1, "Egg, whole, raw, fresh", type: "SR Legacy"), Cand(2, "Egg, whole, raw, fresh", type: "Foundation")])!.Food.FdcId == 2
    && UsdaCatalogMatcher.Score("Banana", Cand(9, "Bananas, dehydrated, or banana powder")).Mismatch == "extra-word:dehydrated" && UsdaCatalogMatcher.Score("Hamburger", Cand(9, "McDONALD'S, Hamburger")).Mismatch == "unsuitable:Branded",
    "USDA verify: Pick bỏ ứng viên bị loại (Oil, almond / glutinous / rice milk), chọn đúng ('Nuts, almonds', 'Rice … regular, cooked', 'Milk, whole'), hoà điểm thì Foundation thắng SR; có lý do loại");

// Món có tương đương rõ ở FDC nhưng tên khác (danh từ nhóm crustaceans/mollusks/leavening agents, đồng nghĩa ghee=butter oil, pomelo=pummelo, mint=spearmint…): phải ra Match
var synonymPairs = new (string Ours, string Usda)[]
{
    ("Shrimp, raw", "Crustaceans, shrimp, raw"), ("Squid", "Mollusks, squid, mixed species, raw"), ("Scallion", "Onions, spring or scallions, (includes tops and bulb), raw"),
    ("Mint, Fresh", "Spearmint, fresh"), ("Cumin, Ground", "Spices, cumin seed"), ("Ghee", "Butter oil, anhydrous"), ("Baking Soda", "Leavening agents, baking soda"),
    ("Baking Powder", "Leavening agents, baking powder, double-acting, sodium aluminum sulfate"), ("Tamari", "Soy sauce made from soy (tamari)"), ("Lemongrass", "Lemon grass (citronella), raw"),
    ("Pomelo", "Pummelo, raw"), ("Cinnamon, Ground", "Spices, cinnamon, ground"),
};
var synonymMissed = synonymPairs.Where(p => UsdaCatalogMatcher.Score(p.Ours, Cand(1, p.Usda)).Grade != UsdaMatchGrade.Match).Select(p => $"{p.Ours} ↔ {p.Usda} ({UsdaCatalogMatcher.Score(p.Ours, Cand(1, p.Usda)).Mismatch})").ToList();
Check(synonymMissed.Count == 0, $"USDA verify hồi quy: {synonymPairs.Length} món có tương đương rõ (Shrimp, Squid, Scallion, Mint, Cumin, Ghee, Baking Soda/Powder, Tamari, Lemongrass, Pomelo) ra Match ({string.Join("; ", synonymMissed)})");
var falseFriends = new (string Ours, string Usda)[]
{
    ("Ghee", "Oil, almond"), ("Ghee", "Butter, salted"), ("Mint, Fresh", "Candies, chocolate mint"), ("Cumin, Ground", "Beef, ground, raw"), ("Tamari", "Soy sauce, reduced sodium"), ("Lemongrass", "Lemon juice, raw"),
    ("Shrimp, raw", "Crustaceans, crab, blue, raw"), ("Baking Soda", "Leavening agents, yeast, baker's, compressed"), ("Scallion", "Onions, raw"), ("Squid", "Fish, mackerel, raw"),
    ("Pomelo", "Grapefruit, raw"), ("Baking Powder", "Leavening agents, baking soda"),
};
var falseMatched = falseFriends.Where(p => UsdaCatalogMatcher.Score(p.Ours, Cand(1, p.Usda)).Grade == UsdaMatchGrade.Match).Select(p => $"{p.Ours} ↔ {p.Usda}").ToList();
Check(falseMatched.Count == 0, $"USDA verify hồi quy: đồng nghĩa/danh từ nhóm mới KHÔNG làm khớp nhầm ({falseFriends.Length} cặp gần giống vẫn không ra Match: {string.Join("; ", falseMatched)})");
var ranked = UsdaCatalogMatcher.Rank("Almonds", [Cand(1, "Oil, almond"), Cand(2, "Nuts, almonds, raw"), Cand(3, "Nuts, almond butter, plain"), Cand(4, "Nuts, almonds", type: "Foundation")]);
Check(ranked.Count == 4 && ranked[0].Food.FdcId == 4 && ranked[1].Food.FdcId == 2 && ranked[^1].Mismatch is not null && ranked.Take(2).All(c => c.Mismatch is null) && UsdaCatalogMatcher.Pick("Almonds", ranked.Select(c => c.Food))!.Food.FdcId == 4,
    "USDA verify: Rank trả mọi ứng viên theo thứ tự Pick (không bị loại trước, điểm, dataset), dùng để in 3 ứng viên tốt nhất");

// Báo cáo verify-usda: review/weak in kèm 3 ứng viên (trước đây các dòng này bị logger bỏ vì lệch số tham số) + CSV
UsdaVerifyCandidate VCand(int id, string desc, decimal score, string? mismatch = null, decimal? kcal = 579m) => new(id, desc, "SR Legacy", score, mismatch, kcal, 21.2m, 49.9m, 21.6m);
UsdaVerifyItem VItem(string key, string status, params UsdaVerifyCandidate[] candidates)
    => new(key, "Almonds", "seed", status, candidates.FirstOrDefault()?.FdcId, candidates.FirstOrDefault()?.Description, "SR Legacy", candidates.FirstOrDefault()?.Score ?? 0, [new UsdaDiff("kcal", 579m, 884m, 0.52m)], [], false, null, candidates, 579m, 21m, 50m, 22m);
var vReport = new UsdaVerifyReport(5, 12,
[
    VItem("almond", UsdaVerifyStatus.Review, VCand(1, "Nuts, almonds", 0.93m), VCand(2, "Nuts, almonds, dry roasted", 0.7m), VCand(3, "Oil, almond", 0.5m, "extra-word:oil", 884m)),
    VItem("tofu", UsdaVerifyStatus.Weak),
    VItem("beef", UsdaVerifyStatus.None, VCand(9, "Beef, ground, raw", 0.4m)),
    VItem("salt", UsdaVerifyStatus.Match, VCand(5, "Salt, table", 0.9m)) with { Applied = true },
    VItem("x", UsdaVerifyStatus.Error) with { Error = "HTTP 400: bad" },
], true);
var vLines = UsdaVerifyReportFormatter.Lines(vReport, true).ToList();
Check(vLines.Any(l => l.StartsWith("  REVIEW almond 'Almonds' (catalog: kcal 579 P 21 F 50 C 22)") && l.Contains("--fdc almond=<fdcId>"))
    && vLines.Contains("      1. FDC 1 [SR Legacy] 'Nuts, almonds' điểm 0.93 · kcal 579 P 21.2 F 49.9 C 21.6") && vLines.Any(l => l.StartsWith("      3. FDC 3") && l.Contains("(loại: extra-word:oil)") && l.Contains("kcal 884"))
    && vLines.Any(l => l.StartsWith("  WEAK   tofu")) && vLines.Contains("      (không có ứng viên từ FDC)") && vLines.Any(l => l.StartsWith("  MATCH  salt") && l.Contains("ĐÃ GHI"))
    && vLines.Any(l => l.StartsWith("  KHÔNG TÌM THẤY beef") && l.Contains("gần nhất FDC 9")) && vLines.Any(l => l.StartsWith("  LỖI    x") && l.Contains("HTTP 400")) && vLines.Any(l => l.StartsWith("Tổng 5 món, 12 request FDC: match 1 · review 1 · weak 1")),
    "Báo cáo verify-usda: REVIEW/WEAK in kèm 3 ứng viên (fdcId, dataType, mô tả, điểm, kcal/P/F/C, lý do loại) và gợi ý --fdc; MATCH/KHÔNG TÌM THẤY/LỖI/tổng vẫn có");
var csvPath = Path.Combine(Path.GetTempPath(), "verify-report-" + Guid.NewGuid().ToString("N") + ".csv");
UsdaVerifyReportFormatter.WriteCsv(csvPath, vReport);
var csvLines = File.ReadAllLines(csvPath);
File.Delete(csvPath);
Check(csvLines[0].EndsWith("fat,carbs,error") && csvLines.Length == 1 + 3 + 1 + 1 + 1 + 1 && csvLines.Count(l => l.StartsWith("review,\"almond\"")) == 3 && csvLines.Any(l => l.StartsWith("weak,\"tofu\"") && l.Contains(",,,,,,,,,,,"))
    && csvLines.Any(l => l.StartsWith("error,\"x\"") && l.EndsWith("\"HTTP 400: bad\"")) && csvLines.Any(l => l.Contains("\"Nuts, almonds, dry roasted\"")),
    "CSV verify-usda: mỗi ứng viên một dòng (review ×3), món không có ứng viên vẫn có một dòng, mô tả có dấu phẩy được bọc nháy, lỗi ghi ở cột error");

// --- USDA client: POST /foods/search với JSON (GET mang ngoặc "Survey (FNDDS)" bị gateway chặn), lỗi 4xx kèm thân lỗi ---
var bodyRef = UsdaClient.BuildSearchBody("Egg, hard-boiled", 10, UsdaDataTypes.Reference);
var bodySurvey = UsdaClient.BuildSearchBody(" Beef pho ", 10, UsdaDataTypes.Survey);
Check(bodyRef == """{"query":"Egg, hard-boiled","dataType":["Foundation","SR Legacy"],"pageSize":10}""" && bodySurvey == """{"query":"Beef pho","dataType":["Survey (FNDDS)"],"pageSize":10}"""
    && UsdaClient.BuildSearchBody("x", 3, "Foundation, SR Legacy ,Survey (FNDDS)").Contains("""["Foundation","SR Legacy","Survey (FNDDS)"]"""),
    "USDA client: thân POST search = JSON {query, dataType:[…], pageSize}, mỗi loại là một phần tử mảng (không còn ngoặc/dấu phẩy trong URL)");
var seen = new List<(HttpMethod Method, string Url, string Body, string Key, string? ContentType)>();
var okHandler = new StubHttpHandler(r =>
{
    seen.Add((r.Method, r.RequestUri!.AbsoluteUri, r.Content is null ? "" : r.Content.ReadAsStringAsync().GetAwaiter().GetResult(), r.Headers.GetValues("X-Api-Key").Single(), r.Content?.Headers.ContentType?.MediaType));
    return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("""{"foods":[{"fdcId":173424},{"fdcId":171287}]}""") };
});
var usdaOk = new UsdaClient(new HttpClient(okHandler) { BaseAddress = new Uri("https://api.nal.usda.gov/fdc/v1/") }, Microsoft.Extensions.Options.Options.Create(new UsdaOptions { ApiKey = "secret-key" }), Microsoft.Extensions.Logging.Abstractions.NullLogger<UsdaClient>.Instance);
var foundIds = usdaOk.SearchAsync("Beef pho", 10, CancellationToken.None, UsdaDataTypes.Survey).GetAwaiter().GetResult();
var sent = seen.Single();
Check(foundIds.SequenceEqual([173424, 171287]) && sent.Method == HttpMethod.Post && sent.Url == "https://api.nal.usda.gov/fdc/v1/foods/search" && sent.Body.Contains("\"Survey (FNDDS)\"") && sent.Body.Contains("\"pageSize\":10")
    && sent.ContentType == "application/json" && !sent.Url.Contains("secret-key") && sent.Key == "secret-key" && usdaOk.SearchAsync("   ", 5, CancellationToken.None).GetAwaiter().GetResult().Count == 0 && seen.Count == 1,
    "USDA client: request thật là POST JSON tới foods/search không có query string, khoá chỉ ở header X-Api-Key, query rỗng không gọi mạng");
var badHandler = new StubHttpHandler(r => new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest) { Content = new StringContent("""{"error":"BAD_REQUEST","message":"dataType invalid"}""") });
var usdaBad = new UsdaClient(new HttpClient(badHandler) { BaseAddress = new Uri("https://api.nal.usda.gov/fdc/v1/") }, Microsoft.Extensions.Options.Options.Create(new UsdaOptions { ApiKey = "k" }), Microsoft.Extensions.Logging.Abstractions.NullLogger<UsdaClient>.Instance);
try { usdaBad.SearchAsync("egg", 5, CancellationToken.None).GetAwaiter().GetResult(); Check(false, "USDA client: 400 phải ném ExternalServiceException"); }
catch (ExternalServiceException ex)
{
    Check(ex.Message.Contains("HTTP 400") && ex.Message.Contains("dataType invalid") && ex.Errors![UsdaClient.UpstreamStatusKey].Single() == "400", "USDA client: lỗi 400 ném kèm mã HTTP + thân lỗi FDC (không còn chỉ 'Bad Request')");
}
var tooMany = new UsdaClient(new HttpClient(new StubHttpHandler(r => new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests) { Content = new StringContent("slow down") })) { BaseAddress = new Uri("https://api.nal.usda.gov/fdc/v1/") },
    Microsoft.Extensions.Options.Options.Create(new UsdaOptions { ApiKey = "k" }), Microsoft.Extensions.Logging.Abstractions.NullLogger<UsdaClient>.Instance);
try { tooMany.GetFoodsAsync([1], CancellationToken.None).GetAwaiter().GetResult(); Check(false, "USDA client: 429 phải ném ExternalServiceException"); }
catch (ExternalServiceException ex) { Check(ex.Message.Contains("429") && ex.Message.Contains("hết hạn mức") && ex.Errors![UsdaClient.UpstreamStatusKey].Single() == "429", "USDA client: 429 báo rõ hết hạn mức"); }

// --- import-usda-bulk: đọc file FDC, lọc "common foods", làm gọn tên, ưu tiên dataset (hàm thuần; ghi DB kiểm ở scratch InMemory) ---
var bulkJson = System.Text.Encoding.UTF8.GetBytes("""
    {"SRLegacyFoods":[
      {"fdcId":1,"description":"Egg, whole, raw","dataType":"SR Legacy","foodCategory":{"description":"Dairy and Egg Products"},
       "foodNutrients":[{"nutrient":{"number":"208","unitName":"kcal"},"amount":1.43E+2},{"nutrient":{"number":"203","unitName":"g"},"amount":12.6},{"nutrient":{"number":"307","unitName":"mg"},"amount":1.42E+2}],
       "foodPortions":[{"amount":1,"gramWeight":50,"modifier":"large","measureUnit":{"name":"undetermined"}}]},
      "not-an-object",
      {"fdcId":2,"description":"Pho, beef, NFS","dataType":"Survey (FNDDS)","wweiaFoodCategory":{"wweiaFoodCategoryDescription":"Soups"},"foodNutrients":[{"nutrient":{"number":"208","unitName":"kcal"},"amount":45}]},
      {"description":"no fdc id"}
    ]}
    """);
var parsedBulk = UsdaFoodParser.ParseDataset(bulkJson, out var datasetName);
Check(datasetName == "SRLegacyFoods" && parsedBulk.Count == 2 && parsedBulk[0].Nutrients.Single(n => n.Number == "208").Amount == 143m && parsedBulk[0].Nutrients.Single(n => n.Number == "307").Amount == 142m
    && parsedBulk[0].Portions.Single() is { Name: "1 large", Grams: 50m } && parsedBulk[1].Category == "Soups" && UsdaFoodParser.ParseDataset("[]"u8, out var noRoot).Count == 0 && noRoot == "",
    "Bulk: đọc file FDC theo khoá gốc, số dạng khoa học (1.43E+2), nhóm WWEIA của FNDDS, phần tử lỗi bị bỏ qua");

// Khẩu phần FNDDS: modifier là MÃ số (90000, 10205), không phải chữ → không được thành tên "1 90000"
var fnddsPortionJson = System.Text.Encoding.UTF8.GetBytes("""
    {"SurveyFoods":[{"fdcId":3,"description":"Milk, whole","dataType":"Survey (FNDDS)","foodNutrients":[],"foodPortions":[
      {"portionDescription":"Quantity not specified","modifier":"90000","gramWeight":244,"measureUnit":{"name":"undetermined"}},
      {"portionDescription":"1 cup","modifier":"10205","gramWeight":244,"measureUnit":{"name":"undetermined"}},
      {"amount":1,"modifier":"10205","gramWeight":240,"measureUnit":{"name":"cup"}},
      {"amount":2,"modifier":"serving","gramWeight":68,"measureUnit":{"name":"undetermined"}}]}]}
    """);
var fnddsPortions = UsdaFoodParser.ParseDataset(fnddsPortionJson, out _).Single().Portions;
Check(fnddsPortions.Select(p => p.Name).SequenceEqual(["1 serving", "1 cup", "2 serving"]) && fnddsPortions.All(p => !UsdaServingFixer.IsBadName(p.Name)) && fnddsPortions[0].Grams == 244m,
    "Khẩu phần FNDDS: 'Quantity not specified' + mã 90000 → '1 serving' (giữ gram); mô tả '1 cup' thắng mã 10205; đơn vị cup + mã số → trùng '1 cup' nên chỉ giữ một");
var fromCode = UsdaFoodParser.ParseDataset(System.Text.Encoding.UTF8.GetBytes("""
    {"SRLegacyFoods":[{"fdcId":5,"description":"Puddings, banana","dataType":"SR Legacy","foodNutrients":[],"foodPortions":[
      {"amount":0.5,"gramWeight":128,"modifier":"cup From 19191","measureUnit":{"name":"undetermined"}},
      {"portionDescription":"1 cup From 19211","gramWeight":250,"measureUnit":{"name":"undetermined"}},
      {"amount":0,"gramWeight":0,"portionDescription":"Quantity not specified","modifier":"90000"}]}]}
    """), out _).Single().Portions;
Check(fromCode.Select(p => p.Name).SequenceEqual(["0.5 cup", "1 cup"]), "Khẩu phần: mã món 'From 19191/19211' bị ghép vào nhãn được bỏ; 'Quantity not specified' có gram 0 bị bỏ");
// "1 RACC" của Foundation → "1 serving"
var raccPortions = UsdaFoodParser.ParseDataset(System.Text.Encoding.UTF8.GetBytes("""
    {"FoundationFoods":[{"fdcId":4,"description":"Hummus, commercial","dataType":"Foundation","foodNutrients":[],"foodPortions":[
      {"amount":1,"gramWeight":30,"measureUnit":{"name":"RACC"},"modifier":null},
      {"amount":2,"gramWeight":33.9,"measureUnit":{"name":"tablespoon"},"modifier":""}]}]}
    """), out _).Single().Portions;
Check(raccPortions.Select(p => p.Name).SequenceEqual(["1 serving", "2 tablespoon"]) && !raccPortions.Any(p => p.Name.Contains("RACC")), "Khẩu phần Foundation: 'RACC' đổi thành 'serving' ('1 serving', không còn '1 RACC')");

// Open Food Facts: tìm theo tên (cgi/search.pl): bỏ sản phẩm thiếu mã hoặc thiếu dinh dưỡng, count là tổng của OFF
var offSearch = OpenFoodFactsParser.ParseSearch(System.Text.Json.JsonDocument.Parse("""
    {"count":1234,"page":1,"products":[
      {"code":"111","product_name":"Apple snack","brands":"Brand A, Brand B","serving_quantity":30,"nutriments":{"energy-kcal_100g":120,"proteins_100g":1,"carbohydrates_100g":25,"fat_100g":2}},
      {"code":"222","product_name":"Không có dinh dưỡng"},
      {"product_name":"Không có mã","nutriments":{"energy-kcal_100g":50}}]}
    """).RootElement);
Check(offSearch.TotalCount == 1234 && offSearch.Items.Count == 1 && offSearch.Items[0] is { Code: "111", Brand: "Brand A", ServingGrams: 30m } && offSearch.Items[0].Nutrition.CaloriesPer100g == 120m
    && OpenFoodFactsParser.ParseSearch(System.Text.Json.JsonDocument.Parse("{}").RootElement).Items.Count == 0, "OFF tìm theo tên: bỏ sản phẩm thiếu mã/dinh dưỡng, lấy thương hiệu đầu, tổng = count của OFF");
Check(UsdaServingFixer.IsBadName("1 90000") && UsdaServingFixer.IsBadName("1.90000") && UsdaServingFixer.IsBadName("0.5 10205") && UsdaServingFixer.IsBadName("2")
    && !UsdaServingFixer.IsBadName("1 cup") && !UsdaServingFixer.IsBadName("100 g") && !UsdaServingFixer.IsBadName("1 large") && !UsdaServingFixer.IsBadName("0.5 cup From 19191"),
    "Nhận diện tên khẩu phần sai: toàn số (kèm mã ≥ 4 chữ số), không bắt nhầm '100 g', '1 cup', '0.5 cup From 19191'");
UsdaFood Common(int id, string desc, string type, string category, decimal? kcal = 100, decimal? protein = 5)
    => new(id, desc, type, category, [], new[] { kcal is { } k ? new UsdaNutrientValue("208", "kcal", k) : null, protein is { } p ? new UsdaNutrientValue("203", "g", p) : null }.OfType<UsdaNutrientValue>().ToList());
Check(UsdaCommonFoods.Reject(Common(1, "Chicken, breast, cooked", "SR Legacy", "Poultry Products")) is null
    && UsdaCommonFoods.Reject(Common(2, "Babyfood, cereal, rice", "SR Legacy", "Baby Foods")) == UsdaSkipReason.BabyOrInfant
    && UsdaCommonFoods.Reject(Common(3, "Cereal, NFS", "Survey (FNDDS)", "Baby food: cereals")) == UsdaSkipReason.BabyOrInfant
    && UsdaCommonFoods.Reject(Common(4, "Spinach, baby, raw", "SR Legacy", "Vegetables and Vegetable Products")) is null && UsdaCommonFoods.Reject(Common(5, "Carrots, baby, raw", "SR Legacy", "Vegetables and Vegetable Products")) is null
    && UsdaCommonFoods.Reject(Common(6, "Candies, NESTLE, BUTTERFINGER Bar", "SR Legacy", "Sweets")) == UsdaSkipReason.Branded && UsdaCommonFoods.Reject(Common(7, "Pillsbury Grands, Biscuits", "SR Legacy", "Baked Products")) == UsdaSkipReason.Branded
    && UsdaCommonFoods.Reject(Common(8, "Beverages, ABBOTT, ENSURE PLUS", "SR Legacy", "Beverages")) == UsdaSkipReason.MedicalFormula
    && UsdaCommonFoods.Reject(Common(9, "Moose, meat, raw (Alaska Native)", "SR Legacy", "American Indian/Alaska Native Foods")) == UsdaSkipReason.RegionalOrCommodity
    && UsdaCommonFoods.Reject(Common(10, "Mystery", "SR Legacy", "Sweets", kcal: null)) == UsdaSkipReason.NoEnergy && UsdaCommonFoods.Reject(Common(11, "Mystery", "SR Legacy", "Sweets", protein: null)) == UsdaSkipReason.NoMacros
    && UsdaCommonFoods.Reject(Common(12, "Hamburger, USDA BBQ sandwich", "SR Legacy", "Fast Foods")) is null,
    "Bulk: lọc common foods (trẻ em, thương hiệu, công thức y tế, món vùng miền, thiếu kcal/macro); 'Spinach, baby' và từ viết tắt USDA/BBQ không bị loại nhầm");
Check(UsdaCommonFoods.CleanName("Pears, raw, bartlett (Includes foods for USDA's Food Distribution Program)") == "Pears, raw, bartlett"
    && UsdaCommonFoods.CleanName("Pho, beef, NFS") == "Pho, beef" && UsdaCommonFoods.CleanName("Pizza, cheese, NS as to type of crust") == "Pizza, cheese"
    && UsdaCommonFoods.CleanName("Mystery stew UPC: 123456789012, NFS") == "Mystery stew" && UsdaCommonFoods.CleanName("Egg, whole, raw, fresh") == "Egg, whole, raw, fresh"
    && UsdaCommonFoods.CleanName("(Alaska Native)") == "(Alaska Native)" && UsdaCommonFoods.CleanName(new string('a', 300)).Length == 256, "Bulk: làm gọn tên (bỏ ngoặc, UPC, NFS, NS as to…), giữ phần còn lại, cắt 256 ký tự");
var planFoods = new[]
{
    Common(30, "Rice, white, raw", "Survey (FNDDS)", "Rice"), Common(20, "Rice, white, raw (Includes foods for USDA's Food Distribution Program)", "SR Legacy", "Cereal Grains and Pasta"), Common(10, "Rice, white, raw", "Foundation", "Cereal Grains and Pasta"),
    Common(40, "Egg", "SR Legacy", "Dairy and Egg Products"), Common(50, "Apple, raw", "SR Legacy", "Fruits"), Common(60, "Already here", "SR Legacy", "Sweets"), Common(70, "Pear, raw", "SR Legacy", "Fruits"),
};
var plan = UsdaCommonFoods.Plan(planFoods, new HashSet<string> { "egg" }, new HashSet<string> { "60" });
Check(plan.Selected.Select(s => s.Food.FdcId).SequenceEqual([10, 50, 70]) && plan.Selected[0].Name == "Rice, white, raw" && plan.Skipped[UsdaSkipReason.DuplicateName] == 2 && plan.Skipped[UsdaSkipReason.ExistsInCatalog] == 1 && plan.Skipped[UsdaSkipReason.AlreadyImported] == 1,
    "Bulk: món trùng tên giữ bản Foundation (rồi SR, rồi FNDDS); trùng tên catalog và fdcId đã nhập bị bỏ qua");
var capped = UsdaCommonFoods.Plan(planFoods, new HashSet<string>(), new HashSet<string>(), 2);
Check(capped.Selected.Count == 2 && capped.Selected[0].Food.FdcId == 10 && capped.Skipped[UsdaSkipReason.OverLimit] == 3 && capped.Samples[UsdaSkipReason.DuplicateName].Count == 2, "Bulk: --max cắt sau khi xếp ưu tiên (phần vượt = OverLimit), báo cáo giữ mẫu từng lý do");

// --- Ghi chú Food Log: validate tên/nội dung/màu ---
bool NoteOk(string text, string? name, NoteColor? color) => new AddFoodLogNoteCommandValidator().Validate(new AddFoodLogNoteCommand(null, text, null, name, color)).IsValid;
Check(NoteOk("x", null, null) && NoteOk(new string('b', 1000), new string('a', 60), NoteColor.Purple) && NoteOk("x", "", NoteColor.Gray) && NoteOk("x", "   ", null),
    "Note: nội dung 1000 + tên 60 (hoặc trống) + màu trong bảng hợp lệ, tên/màu không bắt buộc");
Check(!NoteOk("", null, null) && !NoteOk("  ", null, null) && !NoteOk(new string('b', 1001), null, null) && !NoteOk("x", new string('a', 61), null) && !NoteOk("x", null, (NoteColor)99),
    "Note: nội dung rỗng/1001 ký tự, tên 61 ký tự, màu ngoài enum bị chặn");
Check(new UpdateFoodLogNoteCommandValidator().Validate(new UpdateFoodLogNoteCommand(Guid.NewGuid(), "ok", null, null, NoteColor.Orange)).IsValid
    && !new UpdateFoodLogNoteCommandValidator().Validate(new UpdateFoodLogNoteCommand(Guid.NewGuid(), "", null)).IsValid
    && !new UpdateFoodLogNoteCommandValidator().Validate(new UpdateFoodLogNoteCommand(Guid.NewGuid(), "ok", null, null, (NoteColor)7)).IsValid,
    "Note: sửa dùng cùng quy tắc (nội dung bắt buộc, màu trong bảng)");

// --- Multi-Select: nhãn khẩu phần, gramsPerMl suy từ khẩu phần thể tích, bulk-copy nhiều ngày ---
Check(FoodServingText.Label("1 large - 3 1/4\" diameter") == "large - 3 1/4\" diameter" && FoodServingText.Label("1 cup, sliced") == "cup, sliced" && FoodServingText.Label("100 g") == "100 g"
    && FoodServingText.Label("1.5 cup") == "1.5 cup" && FoodServingText.Label("1") == "1" && FoodServingText.Label("  1 serving ") == "serving",
    "Serving label: bỏ '1 ' đầu, giữ '100 g' / '1.5 cup', mô tả USDA nguyên văn");
FoodItem Vol(decimal? density, params (string Name, decimal Grams, bool Def)[] servings)
{
    var f = new FoodItem { Id = Guid.NewGuid(), Name = "x", DensityGPerMl = density };
    foreach (var (n, g, d) in servings) f.Servings.Add(new FoodServing { Id = Guid.NewGuid(), FoodItemId = f.Id, ServingName = n, WeightInGrams = g, IsDefault = d });
    return f;
}
bool Near(decimal? a, decimal b) => a is { } v && Math.Abs(v - b) < 0.0005m;
Check(Near(FoodServingText.GramsPerMl(Vol(null, ("1 cup, sliced", 109m, false))), 109m / 236.588m)
    && Near(FoodServingText.GramsPerMl(Vol(null, ("1 tbsp", 13.6m, true), ("1 tsp", 4.5m, false))), 13.6m / 14.7868m)
    && Near(FoodServingText.GramsPerMl(Vol(null, ("1/2 cup", 120m, false))), 120m / 118.294m)
    && Near(FoodServingText.GramsPerMl(Vol(null, ("1 1/2 cup", 360m, false))), 360m / 354.882m)
    && Near(FoodServingText.GramsPerMl(Vol(null, ("1 cup, chopped", 100m, true), ("1 cup", 120m, false))), 120m / 236.588m)
    && Near(FoodServingText.GramsPerMl(Vol(null, ("1 fl oz", 30m, false))), 30m / 29.5735m)
    && Near(FoodServingText.GramsPerMl(Vol(1.03m, ("1 cup", 244m, true))), 1.03m),
    "gramsPerMl: suy từ cup/tbsp/tsp/fl oz/phân số, ưu tiên khẩu phần trơn, giá trị lưu trên món thắng");
Check(FoodServingText.GramsPerMl(Vol(null, ("1 medium - 3\" diameter", 182m, true), ("1 oz", 28m, false))) is null
    && FoodServingText.GramsPerMl(Vol(null)) is null && FoodServingText.GramsPerMl(Vol(null, ("1 cup", 5m, true))) is null && FoodServingText.GramsPerMl(Vol(null, ("1 cup", 900m, true))) is null,
    "gramsPerMl: không có khẩu phần thể tích / khối lượng vô lý (0.02, 3.8 g/ml) -> null");
bool CopyOk(int ids, DateOnly? one, int many) => new BulkCopyEntriesCommandValidator().Validate(new BulkCopyEntriesCommand(
    Enumerable.Range(0, ids).Select(_ => Guid.NewGuid()).ToList(), one, null, null,
    many == 0 ? null : Enumerable.Range(1, many).Select(i => new DateOnly(2026, 1, 1).AddDays(i)).ToList())).IsValid;
Check(CopyOk(3, new DateOnly(2026, 1, 1), 0) && CopyOk(3, null, 62) && CopyOk(16, null, 62) && !CopyOk(3, null, 63) && !CopyOk(3, null, 0) && !CopyOk(17, null, 62) && !CopyOk(0, new DateOnly(2026, 1, 1), 0),
    "bulk-copy validator: ToDate cũ hoặc ToDates ≤ 62, tối đa 1000 entry tạo ra (16×62 ok, 17×62 không), phải có ngày và ids");
var td = new BulkCopyEntriesCommand([Guid.NewGuid()], new DateOnly(2026, 1, 5), null, null, [new DateOnly(2026, 1, 9), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 2)]).TargetDates();
Check(td.SequenceEqual([new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 9)]), "bulk-copy: ngày đích gộp ToDate + ToDates, bỏ trùng, tăng dần");

// --- Recipe Preparation: link ≤ 500 http(s), mô tả ≤ 1500, steps ≤ 50 × 300, icon trong bộ ---
bool RecipeOk(string? link = null, string? desc = null, List<RecipeStepInput>? steps = null, string? icon = null) => new CreateRecipeCommandValidator().Validate(new CreateRecipeCommand(
    "R", desc, null, 1, null, null, null, link, null, null, [new RecipeIngredientInput(Guid.NewGuid(), null, 100, null)], steps, icon)).IsValid;
Check(RecipeOk() && RecipeOk("https://example.com/a", new string('d', 1500), [new(null, "x"), new(2, new string('s', 300))], FoodIconKeys.All.First()) && RecipeOk("  ", "")
    && RecipeOk(steps: Enumerable.Range(1, 50).Select(i => new RecipeStepInput(i, "s")).ToList()) && RecipeOk("http://example.com/" + new string('a', 481)),
    "Recipe validator: link http(s) ≤ 500 hoặc trống, mô tả 1500, 50 bước × 300 ký tự, icon trong bộ hợp lệ");
Check(!RecipeOk("ftp://example.com") && !RecipeOk("not a url") && !RecipeOk("https://example.com/" + new string('a', 490)) && !RecipeOk(desc: new string('d', 1501))
    && !RecipeOk(steps: Enumerable.Range(1, 51).Select(i => new RecipeStepInput(i, "s")).ToList()) && !RecipeOk(steps: [new(1, new string('s', 301))]) && !RecipeOk(steps: [new(1, " ")]) && !RecipeOk(icon: "no_such_icon"),
    "Recipe validator: link sai/quá 500, mô tả 1501, 51 bước, bước 301 ký tự/rỗng, icon lạ bị chặn");
Check(!new GetRecipeNutrientsQueryValidator().Validate(new GetRecipeNutrientsQuery([], 1)).IsValid && !new GetRecipeNutrientsQueryValidator().Validate(new GetRecipeNutrientsQuery([new(Guid.NewGuid(), null, 10, null)], 101)).IsValid
    && new GetRecipeNutrientsQueryValidator().Validate(new GetRecipeNutrientsQuery([new(Guid.NewGuid(), null, 10, null)], 4)).IsValid, "Recipe preview validator: ingredients bắt buộc, 1–100 khẩu phần");

// --- MetricSeries: average/difference theo khoảng, bucket Day/Week/Month (trang Scale Weight + Visual Body Fat) ---
var mp = new (DateOnly, decimal)[]
{
    (new DateOnly(2026, 9, 30), 80m), (new DateOnly(2026, 10, 1), 79m), (new DateOnly(2026, 10, 14), 78m), (new DateOnly(2026, 10, 15), 77m),
    (new DateOnly(2026, 10, 19), 76m), (new DateOnly(2026, 10, 20), 75.5m),
};
var (ms, mDay) = MetricSeries.Build(mp, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 20), MetricGranularity.Day, DayOfWeek.Monday);
Check(ms.Count == 6 && ms.Average == 77.58m && ms.Difference == -4.5m && ms.First == 80m && ms.Last == 75.5m && ms.Min == 75.5m && ms.Max == 80m && mDay.Count == 6,
    "MetricSeries: average (mọi điểm) 77.58, difference = cuối − đầu = −4.5, min/max, Day = 6 bucket");
var (_, mWeekMon) = MetricSeries.Build(mp, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 20), MetricGranularity.Week, DayOfWeek.Monday);
Check(mWeekMon.Select(b => (b.Start, b.End, b.Count, b.Average)).SequenceEqual(new[]
    {
        (new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 4), 2, 79.5m), (new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 18), 2, 77.5m), (new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 25), 2, 75.75m),
    }), "MetricSeries Week (bắt đầu thứ Hai): 28/9–4/10, 12–18/10, 19–25/10 với trung bình mỗi tuần");
var (_, mWeekSun) = MetricSeries.Build(mp, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 20), MetricGranularity.Week, DayOfWeek.Sunday);
Check(mWeekSun.Select(b => (b.Start, b.Count)).SequenceEqual(new[] { (new DateOnly(2026, 9, 27), 2), (new DateOnly(2026, 10, 11), 2), (new DateOnly(2026, 10, 18), 2) }),
    "MetricSeries Week (bắt đầu Chủ nhật): bucket dịch theo ngày đầu tuần của user");
var (_, mMonth) = MetricSeries.Build(mp, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 20), MetricGranularity.Month, DayOfWeek.Monday);
Check(mMonth.Select(b => (b.Start, b.End, b.Count, b.Average)).SequenceEqual(new[]
    {
        (new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), 1, 80m), (new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), 5, 77.1m),
    }), "MetricSeries Month: tháng 9 một điểm, tháng 10 trung bình 5 điểm = 77.1");
var (mOne, _) = MetricSeries.Build(mp, new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 20), null, DayOfWeek.Monday);
var (mNone, mNoneBuckets) = MetricSeries.Build(mp, new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31), MetricGranularity.Week, DayOfWeek.Monday);
var (mCut, _) = MetricSeries.Build(mp, new DateOnly(2026, 10, 14), new DateOnly(2026, 10, 19), null, DayOfWeek.Monday);
Check(mOne.Count == 1 && mOne.Average == 75.5m && mOne.Difference is null && mNone.Count == 0 && mNone.Average is null && mNone.Difference is null && mNoneBuckets.Count == 0
    && mCut.Count == 3 && mCut.Average == 77m && mCut.Difference == -2m, "MetricSeries: một điểm → difference null; khoảng rỗng → count 0; khoảng cắt đúng biên cả hai đầu");

// --- Full Body / từng số đo: danh mục, patch từng phần ---
var cat = BodyMeasurements.All;
var roundTrip = true;
var probe = new BodyMetric();
var modelProps = typeof(TapeMeasurementsModel).GetProperties().Where(p => p.PropertyType == typeof(decimal?)).Select(p => p.Name).ToHashSet();
foreach (var (m, i) in cat.Select((m, i) => (m, i)))
{
    var value = 20m + i;
    m.Set(probe, value);
    roundTrip &= m.Get(probe) == value && cat.Count(o => o.Get(probe) is not null) == 1 + 0 * i;
    m.Set(probe, null);
    roundTrip &= !BodyMeasurements.HasAny(probe);
}
Check(cat.Count == 18 && cat.Select(m => m.Key).Distinct().Count() == 18 && cat.Select(m => m.ModelName).ToHashSet().SetEquals(modelProps) && cat.All(m => m.Min > 0 && m.Min < m.Max) && roundTrip
    && cat.Select(m => m.Group).Distinct().OrderBy(x => x).SequenceEqual(new[] { "Arms", "Legs", "UpperBody" }),
    "BodyMeasurements: 18 số đo, khoá/tên trường duy nhất, đủ trường TapeMeasurementsModel, đọc/ghi từng cột đúng, 3 nhóm");
var tapeSample = new TapeMeasurementsModel(38m, null, null, null, 90m, null, 33m, null, null, null, null, null, null, null, null, null, null, null);
var chg = new PatchFullBodyCommand(null, 22m, tapeSample, ["hips", "visualBodyFat2"]).Changes();
Check(chg.Count == 6 && chg["neck"] == 38m && chg["waist"] == 90m && chg["leftBicep"] == 33m && chg["visualBodyFat"] == 22m && chg["hips"] is null && chg["visualBodyFat2"] is null,
    "Patch Changes(): ô gửi = ghi, clear = null (khoá lạ giữ nguyên để validator bắt)");
bool PatchOk(decimal? visual, TapeMeasurementsModel? tape, List<string>? clear) => new PatchFullBodyCommandValidator().Validate(new PatchFullBodyCommand(null, visual, tape, clear)).IsValid;
Check(PatchOk(null, tapeSample, null) && PatchOk(22m, null, null) && PatchOk(null, null, ["waist", "visualBodyFat", "LeftBicep"]) && PatchOk(null, tapeSample, ["hips"]),
    "Patch validator: chỉ ghi, chỉ xoá (khoá không phân biệt hoa thường), vừa ghi ô này vừa xoá ô khác đều hợp lệ");
Check(!PatchOk(null, null, null) && !PatchOk(null, null, []) && !PatchOk(null, tapeSample, ["waist"]) && !PatchOk(null, null, ["elbow"]) && !PatchOk(71m, null, null) && !PatchOk(1m, null, null)
    && !PatchOk(null, new TapeMeasurementsModel(120m, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null), null)
    && !PatchOk(null, new TapeMeasurementsModel(null, null, null, null, 30m, null, null, null, null, null, null, null, null, null, null, null, null, null), null),
    "Patch validator: rỗng, vừa ghi vừa xoá cùng khoá, khoá lạ, % mỡ ngoài 2–70, cổ 120 cm, eo 30 cm bị chặn");
Check(new UpsertMeasurementCommandValidator().Validate(new UpsertMeasurementCommand("waist", null, 88.5m)).IsValid && new UpsertMeasurementCommandValidator().Validate(new UpsertMeasurementCommand("RightAnkle", null, 22m)).IsValid
    && !new UpsertMeasurementCommandValidator().Validate(new UpsertMeasurementCommand("waist", null, 20m)).IsValid && !new UpsertMeasurementCommandValidator().Validate(new UpsertMeasurementCommand("elbow", null, 30m)).IsValid
    && !new UpsertMeasurementCommandValidator().Validate(new UpsertMeasurementCommand("rightAnkle", null, 80m)).IsValid, "Upsert số đo validator: khoảng theo từng số đo, khoá không biết bị chặn");

// --- Model binding: thuộc tính [JsonIgnore] do route gán sau khi bind body phải nullable ---
// Non-nullable reference type + [ApiController] => model validation báo "The X field is required" (400) trước khi controller kịp gán (lỗi POST measurements/{key}, PUT nutrients/{code}/target)
var nullCtx = new System.Reflection.NullabilityInfoContext();
var jsonIgnoredNonNull = typeof(CreateRecipeCommand).Assembly.GetTypes()
    .Where(t => typeof(MediatR.IBaseRequest).IsAssignableFrom(t))
    .SelectMany(t => t.GetProperties().Where(p => p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), true).Length > 0)
        .Where(p => !p.PropertyType.IsValueType && nullCtx.Create(p).WriteState == System.Reflection.NullabilityState.NotNull)
        .Select(p => $"{t.Name}.{p.Name}"))
    .ToList();
Check(jsonIgnoredNonNull.Count == 0, $"Model binding: không còn thuộc tính [JsonIgnore] non-nullable trong request ({string.Join(", ", jsonIgnoredNonNull)})");
static bool FlagsNonNullJsonIgnore(Type t) => t.GetProperties().Any(p => p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), true).Length > 0
    && !p.PropertyType.IsValueType && new System.Reflection.NullabilityInfoContext().Create(p).WriteState == System.Reflection.NullabilityState.NotNull);
Check(FlagsNonNullJsonIgnore(typeof(BadBindProbe)) && !FlagsNonNullJsonIgnore(typeof(GoodBindProbe)), "Model binding: bộ kiểm tự thử đúng (bắt được string Key non-nullable, bỏ qua string? Key)");

// --- Weight Trend: trend MỖI NGÀY (EMA α = 2/(N+1), nội suy tuyến tính khoảng trống) + Insights ---
var t0 = new DateOnly(2026, 10, 1);
DateOnly Td(int d) => t0.AddDays(d);
const int N = 20;
decimal Alpha = 2m / (N + 1);
// Cùng thuật toán với WeightTrendCalculator.Compute ở ngày có cân (dãy có khoảng trống, ngày thưa, không theo thứ tự)
var series = new (DateOnly, decimal)[] { (Td(9), 78.9m), (Td(0), 80m), (Td(3), 79.4m), (Td(4), 79.8m), (Td(15), 77.5m) };
var stored = WeightTrendCalculator.Compute(series, N);
var dailyRaw = WeightTrendSeries.Daily(series, N, Td(15));
Check(dailyRaw.Count == 16 && dailyRaw.All(p => p.Scale is null || stored[p.Date] == Math.Round(p.Trend, 2)) && dailyRaw.Where(p => p.Scale is not null).Count() == 5
    && dailyRaw.Select((p, i) => p.Date == Td(i)).All(x => x), "Trend ngày: liên tục từ lần cân đầu, đúng bằng trend lưu theo từng lần cân (cùng EMA), ngày không cân có Scale = null");
// Ảnh MF (04): trend 79.3 phẳng nhiều ngày, hôm nay cân 78.3 → trend 79.2 (−0.1); N=20 → α = 2/21
var mf = Enumerable.Range(0, 6).Select(i => (Td(i), 79.3m)).Append((Td(6), 78.3m)).ToList();
var mfDays = WeightTrendSeries.ToDays(WeightTrendSeries.Daily(mf, N, Td(6)));
Check(mfDays.Take(6).All(d => d.TrendKg == 79.30m) && mfDays[6].TrendKg == 79.20m && mfDays[6].ChangeKg == -0.10m && mfDays[5].ChangeKg == 0m && mfDays[0].ChangeKg is null && mfDays[6].ScaleKg == 78.30m,
    $"Khớp ảnh MacroFactor: 79.3 phẳng rồi cân 78.3 → trend {mfDays[6].TrendKg} (−0.1), ngày đầu không có thay đổi");
// 1 điểm: trend phẳng bằng số cân tới hôm nay
var one = WeightTrendSeries.Daily([(Td(0), 80m)], N, Td(5));
var oneI = WeightTrendSeries.Insights(one, Td(5), N, 7700);
Check(one.Count == 6 && one.All(p => p.Trend == 80m) && one[0].Scale == 80m && one.Skip(1).All(p => p.Scale is null)
    && oneI.CurrentWeightKg == 80m && oneI.WeeklyChangeKg is null && oneI.EnergyDeficitKcalPerDay is null && oneI.Projected30DaysKg is null && oneI.DataDays == 6 && oneI.FirstWeighInDate == Td(0)
    && oneI.WeightChanges[0] is { Days: 3, ChangeKg: 0m, Status: "NoChange" } && oneI.WeightChanges.Skip(1).All(c => c.Status == "Waiting" && c.ChangeKg is null),
    "1 điểm cân (6 ngày dữ liệu): trend phẳng mỗi ngày, chỉ 3-day có giá trị (không đổi), 7/14/30/90 Waiting, tuần/thâm hụt/dự kiến = null");
Check(WeightTrendSeries.Daily([], N, Td(5)).Count == 0 && WeightTrendSeries.Insights([], Td(5), N, 7700) is { CurrentWeightKg: null, FirstWeighInDate: null, DataDays: 0 } && WeightTrendSeries.Daily([(Td(9), 80m)], N, Td(5)).Count == 0,
    "Chưa có cân (hoặc cân đầu tiên sau hôm nay): chuỗi rỗng, Insights rỗng");
// Khoảng trống: 80 → 78 trong 4 ngày; ngày giữa = giá trị nội suy 79.5 / 79 / 78.5 chạy qua EMA
var gap = WeightTrendSeries.Daily([(Td(0), 80m), (Td(4), 78m)], N, Td(4));
decimal manual = 80m;
foreach (var kg in new[] { 79.5m, 79m, 78.5m, 78m }) manual = Alpha * kg + (1 - Alpha) * manual;
Check(gap.Count == 5 && gap[4].Trend == manual && gap[1].Scale is null && gap[1].Trend < 80m && gap[2].Trend < gap[1].Trend && gap[3].Trend < gap[2].Trend, "Khoảng trống giữa hai lần cân: mỗi ngày giữa có trend (EMA chạy qua giá trị nội suy tuyến tính), giảm dần");
// Cân lại sau nhiều ngày nghỉ (30 ngày): vẫn liên tục, trend ngày cân lại bằng EMA lưu trữ
var longGap = new (DateOnly, decimal)[] { (Td(0), 80m), (Td(30), 76m) };
var lg = WeightTrendSeries.Daily(longGap, N, Td(30));
Check(lg.Count == 31 && lg.Select((p, i) => i == 0 || p.Trend < lg[i - 1].Trend).All(x => x) && Math.Round(lg[30].Trend, 2) == WeightTrendCalculator.Compute(longGap, N)[Td(30)] && lg.Count(p => p.Scale is not null) == 2,
    "Cân lại sau 30 ngày nghỉ: 31 ngày liên tục, trend giảm đều, ngày cân lại khớp trend lưu");
// Hôm nay chưa cân: trend hôm nay vẫn có, tiến dần về cân cuối (giữ cân cuối), Scale = null
var untilToday = WeightTrendSeries.Daily([(Td(0), 80m), (Td(1), 78m)], N, Td(6));
decimal held = untilToday[1].Trend;
for (var i = 2; i <= 6; i++) held = Alpha * 78m + (1 - Alpha) * held;
Check(untilToday.Count == 7 && untilToday[6].Scale is null && untilToday[6].Trend == held && untilToday[6].Trend < untilToday[1].Trend && untilToday[6].Trend > 78m,
    "Hôm nay chưa cân: vẫn có trend, EMA tiếp tục với cân cuối (tiến dần về 78, chưa tới), ngày không cân Scale = null");
Check(WeightTrendSeries.Daily([(Td(0), 80m), (Td(5), 79m)], N, Td(3)).Last().Date == Td(3) && WeightTrendSeries.Daily([(Td(0), 80m), (Td(5), 79m)], N, Td(3)).Count == 4, "Ngày kết thúc cắt giữa hai lần cân: chỉ tới ngày kết thúc");
// Insights: giảm tuyến tính 0.02 kg/ngày trong 21 ngày (đường trend thẳng)
var line = Enumerable.Range(0, 21).Select(i => new TrendPoint(Td(i), 80m - 0.02m * i, null)).ToList();
var ins = WeightTrendSeries.Insights(line, Td(20), N, 7700);
Check(ins.CurrentWeightKg == 79.60m && ins.WeeklyChangeKg == -0.14m && ins.EnergyDeficitKcalPerDay == -154m && ins.Projected30DaysKg == 79.60m - 0.6m
    && ins.WeightChanges.Select(c => (c.Days, c.ChangeKg, c.Status)).SequenceEqual(new (int, decimal?, string)[] { (3, -0.06m, "Decrease"), (7, -0.14m, "Decrease"), (14, -0.28m, "Decrease"), (30, null, "Waiting"), (90, null, "Waiting") }),
    $"Insights (dốc −0.02 kg/ngày): hiện tại 79.60, −0.14 kg/tuần, thâm hụt −154 kcal/ngày (×7700÷7), dự kiến 78.99 sau 30 ngày; 3/7/14 ngày có giá trị, 30/90 Waiting ({ins.WeeklyChangeKg}, {ins.EnergyDeficitKcalPerDay}, {ins.Projected30DaysKg})");
var flat = Enumerable.Range(0, 10).Select(i => new TrendPoint(Td(i), 80m - 0.01m * i, null)).ToList();
Check(WeightTrendSeries.Insights(flat, Td(9), N, 7700).WeightChanges[0] is { Days: 3, Status: "NoChange", ChangeKg: -0.03m } && WeightTrendSeries.Insights(flat, Td(9), N, 7700).WeightChanges[1] is { Days: 7, Status: "Decrease", ChangeKg: -0.07m },
    "Weight Changes: |thay đổi| < 0.05 kg là NoChange, từ 0.05 là Decrease");
var six = Enumerable.Range(0, 6).Select(i => new TrendPoint(Td(i), 80m - 0.02m * i, null)).ToList();
var seven = Enumerable.Range(0, 7).Select(i => new TrendPoint(Td(i), 80m - 0.02m * i, null)).ToList();
var eight = Enumerable.Range(0, 8).Select(i => new TrendPoint(Td(i), 80m - 0.02m * i, null)).ToList();
Check(WeightTrendSeries.Insights(six, Td(5), N, 7700).WeeklyChangeKg is null && WeightTrendSeries.Insights(seven, Td(6), N, 7700).WeeklyChangeKg == -0.14m
    && WeightTrendSeries.Insights(seven, Td(6), N, 7700).WeightChanges[1].Status == "Waiting" && WeightTrendSeries.Insights(eight, Td(7), N, 7700).WeightChanges[1] is { Status: "Decrease", ChangeKg: -0.14m },
    "Điều kiện dữ liệu: tuần cần ≥ 7 ngày trend; 7-day change cần ngày hôm nay − 7 đã có trend (Waiting đến ngày thứ 8) như MacroFactor");
// Hồi quy chỉ dùng 21 ngày gần nhất: 40 ngày, 19 ngày đầu dốc −0.1, 21 ngày cuối phẳng → weekly ≈ 0
var bend = Enumerable.Range(0, 40).Select(i => new TrendPoint(Td(i), i < 19 ? 80m - 0.1m * i : 80m - 0.1m * 18, null)).ToList();
Check(WeightTrendSeries.Insights(bend, Td(39), N, 7700).WeeklyChangeKg == 0m, "Hồi quy chỉ dùng 21 ngày gần nhất (đoạn dốc cũ không ảnh hưởng)");

// --- Expenditure series: chuỗi liên tục tới hôm nay (ngày thiếu snapshot giữ TDEE), Flux, change, Insights ---
ExpenditureSnapshotRow Snap(int d, decimal tdee, string status = "Updating", int window = 21) => new(Td(d), tdee, status, window);
var snapRows = new[] { Snap(0, 2000m, "Calibrating", 3), Snap(1, 2010m, "Calibrating", 4), Snap(2, 2020m), Snap(3, 2020m, "Holding") };
var expDaily = ExpenditureSeries.Daily(snapRows, Td(7), Td(-30), 14);
Check(expDaily.Count == 8 && expDaily.Zip(expDaily.Skip(1)).All(p => p.Second.Date == p.First.Date.AddDays(1)) && expDaily[0].Date == Td(0) && expDaily[^1].Date == Td(7),
    "Expenditure series: liên tục từ snapshot đầu tới hôm nay");
Check(expDaily.Skip(4).All(p => p.Provisional && p.Tdee == 2020m && p.Status == "Holding" && p.WindowDays == 21) && expDaily.Take(4).All(p => !p.Provisional),
    "Ngày sau snapshot cuối giữ TDEE gần nhất, Holding, giữ cửa sổ dữ liệu");
Check(ExpenditureSeries.Daily(snapRows, Td(7), Td(-5), 14).Skip(4).All(p => p.Status == "Calibrating") && ExpenditureSeries.Daily(snapRows, Td(7), null, 14).Skip(4).All(p => p.Status == "Holding"),
    "Ngày thiếu snapshot còn trong giai đoạn học (< 14 ngày từ start): Calibrating; không có start: Holding");
var gapDaily = ExpenditureSeries.Daily([Snap(0, 2000m), Snap(3, 2100m)], Td(4), Td(-30), 14);
Check(gapDaily.Count == 5 && gapDaily[1].Tdee == 2000m && gapDaily[2].Tdee == 2000m && gapDaily[1].Provisional && gapDaily[3].Tdee == 2100m && gapDaily[4].Tdee == 2100m, "Khoảng trống giữa hai snapshot giữ TDEE ngày trước");
Check(ExpenditureSeries.Daily([], Td(5), Td(-30), 14).Count == 0 && ExpenditureSeries.Daily([Snap(9, 2000m)], Td(5), Td(-30), 14).Count == 0 && ExpenditureSeries.Daily([Snap(0, 2000m), Snap(9, 2100m)], Td(5), Td(-30), 14).Count == 6,
    "Không có snapshot / chỉ có snapshot tương lai: rỗng; snapshot sau hôm nay bị bỏ");
var toDays = ExpenditureSeries.ToDays(ExpenditureSeries.Daily([Snap(0, 2000.4m, "Updating", 21), Snap(1, 2000.8m, "Updating", 21), Snap(2, 2100m, "Calibrating", 2)], Td(2), null, 14), 7700, 0.1m);
Check(toDays[0] is { ExpenditureKcal: 2000m, FluxLowKcal: 1963m, FluxHighKcal: 2037m, ChangeKcal: null } && toDays[1] is { ExpenditureKcal: 2001m, ChangeKcal: 0m }, "ToDays: kcal làm tròn, Flux = ± 37 ở cửa sổ 21 ngày, change tính từ giá trị chưa làm tròn (0.4 → 0)");
Check(toDays[2] is { FluxLowKcal: 1990m, FluxHighKcal: 2210m, ChangeKcal: 99m } && ExpenditureCalculator.FluxHalfWidth(0, 7700, 0.1m) == ExpenditureCalculator.FluxHalfWidth(7, 7700, 0.1m), "Flux rộng hơn khi ít dữ liệu (cửa sổ < 7 ngày tính như 7: ± 110)");
var insRows = Enumerable.Range(0, 40).Select(i => Snap(i, 2500m - i * 5m)).ToList();
var rawIns = ExpenditureSeries.Daily(insRows, Td(39), Td(-30), 14);
List<ExpenditureDay> Last7(int logged, int weighed) => Enumerable.Range(0, 7).Select(i => new ExpenditureDay(Td(i), i < logged ? 2000m : null, i < weighed ? 80m : null)).ToList();
var insA = ExpenditureSeries.Insights(rawIns, Td(39), Td(-30), 14, Last7(7, 7));
Check(insA.CurrentKcal == 2305m && insA.Strategy == "Updating" && insA.FirstDate == Td(0) && insA.HoldingReason is null && insA.HoldingReasonCodes.Count == 0 && insA.CalibratingDaysLeft is null && insA.LoggedDaysLast7 == 7 && insA.WeighInDaysLast7 == 7,
    "Insights: TDEE hôm nay, Strategy = status của điểm cuối, không lý do khi Updating");
Check(insA.Changes.Select(c => c.Days).SequenceEqual([3, 7, 14, 30, 90]) && insA.Changes[0] is { ChangeKcal: -15m, Status: "Decrease" } && insA.Changes[3] is { ChangeKcal: -150m, Status: "Decrease" } && insA.Changes[4] is { ChangeKcal: null, Status: "Waiting" },
    "Changes: TDEE hôm nay − N ngày trước (3 ngày −15, 30 ngày −150, Decrease); mốc 90 Waiting vì chuỗi chỉ 40 ngày");
Check(ExpenditureSeries.Insights(ExpenditureSeries.Daily([Snap(0, 2000m), Snap(1, 2000.3m), Snap(2, 1900m)], Td(2), Td(-30), 14), Td(2), Td(-30), 14, Last7(7, 7)).Changes[0] is { Days: 3, Status: "Waiting" },
    "Mốc nằm trước ngày đầu của chuỗi: Waiting");
var flatRaw = ExpenditureSeries.Daily(Enumerable.Range(0, 10).Select(i => Snap(i, i == 9 ? 2000.4m : 2000m)).ToList(), Td(9), Td(-30), 14);
var declRaw = ExpenditureSeries.Daily(Enumerable.Range(0, 10).Select(i => Snap(i, 2000m - i * 10m)).ToList(), Td(9), Td(-30), 14);
Check(ExpenditureSeries.Insights(flatRaw, Td(9), Td(-30), 14, Last7(7, 7)).Changes[0] is { Status: "NoChange", ChangeKcal: 0m } && ExpenditureSeries.Insights(declRaw, Td(9), Td(-30), 14, Last7(7, 7)).Changes[0] is { Status: "Decrease", ChangeKcal: -30m },
    "Status change: làm tròn = 0 là NoChange, giảm là Decrease");
var holdRaw = ExpenditureSeries.Daily([Snap(0, 2000m), Snap(1, 2000m, "Holding")], Td(1), Td(-30), 14);
var holdA = ExpenditureSeries.Insights(holdRaw, Td(1), Td(-30), 14, Last7(2, 7));
var holdB = ExpenditureSeries.Insights(holdRaw, Td(1), Td(-30), 14, Last7(7, 0));
var holdC = ExpenditureSeries.Insights(holdRaw, Td(1), Td(-30), 14, Last7(2, 0));
Check(holdA.Strategy == "Holding" && holdA.HoldingReasonCodes.SequenceEqual(["MissingLogs"]) && holdA.HoldingReason is not null && holdB.HoldingReasonCodes.SequenceEqual(["NoRecentWeighIn"]) && holdC.HoldingReasonCodes.SequenceEqual(["MissingLogs", "NoRecentWeighIn"]) && holdA.LoggedDaysLast7 == 2,
    "Holding: MissingLogs khi thiếu > 3/7 ngày, NoRecentWeighIn khi không cân; có thể cả hai");
var calRaw = ExpenditureSeries.Daily([Snap(0, 2000m, "Calibrating")], Td(0), Td(-5), 14);
var calIns = ExpenditureSeries.Insights(calRaw, Td(0), Td(-5), 14, Last7(1, 1));
Check(calIns.Strategy == "Calibrating" && calIns.CalibratingDaysLeft == 9 && calIns.HoldingReason is null && calIns.HoldingReasonCodes.Count == 0, "Calibrating: còn 14 − 5 = 9 ngày, không có lý do Holding");
var noneIns = ExpenditureSeries.Insights([], Td(0), null, 14, Last7(0, 0));
Check(noneIns.CurrentKcal is null && noneIns.Strategy is null && noneIns.FirstDate is null && noneIns.Changes.Count == 5 && noneIns.Changes.All(c => c.Status == "Waiting"), "Insights khi chưa có snapshot: rỗng, mọi change Waiting");

// --- Nutrition Data Manager: validator lưu một ngày ---
var ndmV = new SaveNutritionDataCommandValidator();
SaveNutritionDataCommand Ndm(decimal? kcal, decimal? p = null, decimal? f = null, decimal? c = null, bool fasting = false, DateOnly? date = null) => new(date ?? Td(0), kcal, p, f, c, fasting, false);
Check(ndmV.Validate(Ndm(2000m, 150m, 60m, 200m)).IsValid && ndmV.Validate(Ndm(0m)).IsValid && ndmV.Validate(Ndm(null)).IsValid && ndmV.Validate(Ndm(null, fasting: true)).IsValid && ndmV.Validate(Ndm(0m, 0m, 0m, 0m, true)).IsValid,
    "NDM validator: nhập đủ / chỉ kcal / chỉ cờ / nhịn ăn (trống hoặc 0) hợp lệ");
Check(!ndmV.Validate(Ndm(500m, fasting: true)).IsValid && !ndmV.Validate(Ndm(null, 10m)).IsValid && !ndmV.Validate(Ndm(10001m)).IsValid && !ndmV.Validate(Ndm(100m, 1001m)).IsValid && !ndmV.Validate(Ndm(100m, c: -1m)).IsValid && !ndmV.Validate(Ndm(100m) with { Date = null }).IsValid,
    "NDM validator: nhịn ăn kèm số, macro không kèm kcal, ngoài khoảng, thiếu ngày bị chặn");
Check(new GetNutritionDataQueryValidator().Validate(new GetNutritionDataQuery(Td(1), Td(0))).IsValid == false && new GetNutritionDataQueryValidator().Validate(new GetNutritionDataQuery(Td(0), Td(0))).IsValid && new GetNutritionDataQueryValidator().Validate(new GetNutritionDataQuery(null, Td(0))).IsValid
    && !new GetExpenditureSeriesQueryValidator().Validate(new GetExpenditureSeriesQuery(Td(1), Td(0))).IsValid && new GetExpenditureSeriesQueryValidator().Validate(new GetExpenditureSeriesQuery(null, Td(0))).IsValid,
    "Validator khoảng ngày: from > to bị chặn, from = to / thiếu from hợp lệ");
Check(NutritionDataSupport.SourceOf(false, 2, 1) == "App" && NutritionDataSupport.SourceOf(false, 2, 2) == "Manual" && NutritionDataSupport.SourceOf(true, 0, 0) == "Manual" && NutritionDataSupport.SourceOf(false, 1, 0) == "App",
    "Nguồn của ngày: toàn Quick Add hoặc nhịn ăn = Manual, còn lại App");

Check(NutritionDataSupport.IsAppDay([new FoodLogEntry { Source = EntrySource.Search }]) && NutritionDataSupport.IsAppDay([new FoodLogEntry { Source = EntrySource.QuickAdd }, new FoodLogEntry { Source = EntrySource.Recipe }])
    && !NutritionDataSupport.IsAppDay([new FoodLogEntry { Source = EntrySource.QuickAdd }]) && !NutritionDataSupport.IsAppDay([]), "Ngày App = có ít nhất một entry không phải Quick Add (NDM không ghi đè)");

// --- Energy Balance: kcal ăn vs expenditure vs target CỦA NGÀY, trung bình mỗi ngày đã log, Relative 3/7/14/30/90 ---
FoodLogSupport.LoggedDay Lg(int d, decimal kcal, bool fasting = false, bool incomplete = false) => new(Td(d), kcal, 0, 0, 0, 0, 0, incomplete, fasting);
var ebToday = 20;
// Mô phỏng ảnh MacroFactor (4 ngày log cuối, expenditure 2429): chênh lệch −2160, −2332, −958, +833; target 2877 (2884 hôm nay): −2608, −2780, −1406, +378
var ebLogs = new[] { Lg(17, 269m), Lg(18, 97m), Lg(19, 1471m), Lg(20, 3262m) }.ToDictionary(l => l.Date);
var ebExp = Enumerable.Range(0, 21).ToDictionary(i => Td(i), _ => 2429m);
var ebDays = EnergyBalanceCalculator.Days(Td(0), Td(ebToday), ebLogs, ebExp, d => d == Td(20) ? 2884m : 2877m);
Check(ebDays.Count == 21 && ebDays[0].Date == Td(0) && ebDays[^1].Date == Td(20) && ebDays.Take(17).All(d => d.CaloriesKcal is null && d.VsExpenditureKcal is null && d.VsTargetKcal is null && d.ExpenditureKcal == 2429m && d.TargetKcal == 2877m),
    "Energy Balance: mọi ngày liên tục; ngày chưa log có calories/chênh lệch null nhưng vẫn có expenditure và target");
Check(ebDays[17] is { CaloriesKcal: 269m, VsExpenditureKcal: -2160m, VsTargetKcal: -2608m } && ebDays[20] is { VsExpenditureKcal: 833m, VsTargetKcal: 378m, TargetKcal: 2884m }, "Chênh lệch ngày = calories − expenditure / target của đúng ngày (target đổi theo ngày)");
var relExp = EnergyBalanceCalculator.Relative(ebDays, Td(ebToday), false);
var relTgt = EnergyBalanceCalculator.Relative(ebDays, Td(ebToday), true);
Check(relExp.Select(r => r.Days).SequenceEqual([3, 7, 14, 30, 90]) && relExp[0] is { AvgKcal: -819m, LoggedDays: 3, Status: "Deficit" } && relExp.Skip(1).All(r => r is { AvgKcal: -1154m, LoggedDays: 4, Status: "Deficit" }),
    "Relative to Expenditure: 3-day −819 (3 ngày log), 7/14/30/90-day −1154 (trung bình mỗi ngày ĐÃ LOG, không chia cho độ dài cửa sổ)");
Check(relTgt[0] is { AvgKcal: -1269m, LoggedDays: 3, Status: "BelowTarget" } && relTgt.Skip(1).All(r => r is { AvgKcal: -1604m, LoggedDays: 4, Status: "BelowTarget" }), "Relative to Targets: 3-day −1269, 7+ day −1604, BelowTarget");
var ebSum = EnergyBalanceCalculator.Summary(ebDays, Td(14), Td(20));
Check(ebSum is { LoggedDays: 4, AvgCaloriesKcal: 1275m, AvgExpenditureKcal: 2429m, AvgVsExpenditureKcal: -1154m, TotalVsExpenditureKcal: -4617m, VsExpenditureDays: 4, VsExpenditureStatus: "Deficit", AvgVsTargetKcal: -1604m, VsTargetStatus: "BelowTarget" },
    "Summary 1W: trung bình mỗi ngày đã log (calories 1275, vs expenditure −1154, tổng −4617, vs target −1604)");
Check(EnergyBalanceCalculator.Relative(ebDays, Td(19), false)[0] is { AvgKcal: -1817m, LoggedDays: 3 }, "Cửa sổ N ngày gồm hôm nay và N−1 ngày trước (ngày thứ N+1 bị loại): 3-day tới ngày 19 = ngày 17..19");
// Nhịn ăn = ngày log 0 kcal (được tính, kéo trung bình xuống); chưa đủ = hiện số nhưng KHÔNG tính; chưa có expenditure/target thì ngày chỉ góp calories
var ebLogs2 = new[] { Lg(10, 0m, fasting: true), Lg(11, 5000m, incomplete: true), Lg(12, 2429m) }.ToDictionary(l => l.Date);
var ebDays2 = EnergyBalanceCalculator.Days(Td(8), Td(12), ebLogs2, new Dictionary<DateOnly, decimal> { [Td(10)] = 2429m, [Td(11)] = 2429m, [Td(12)] = 2429m }, d => d >= Td(11) ? 2000m : null);
Check(ebDays2[2] is { CaloriesKcal: 0m, IsFasting: true, VsExpenditureKcal: -2429m, VsTargetKcal: null } && ebDays2[3] is { CaloriesKcal: 5000m, IsIncomplete: true, VsExpenditureKcal: 2571m } && !EnergyBalanceCalculator.Counts(ebDays2[3]) && EnergyBalanceCalculator.Counts(ebDays2[2]),
    "Nhịn ăn: 0 kcal tính là đã log; ngày chưa đủ giữ số + cờ nhưng không được tính");
var ebSum2 = EnergyBalanceCalculator.Summary(ebDays2, Td(8), Td(12));
Check(ebSum2 is { LoggedDays: 2, AvgCaloriesKcal: 1215m, AvgVsExpenditureKcal: -1215m, TotalVsExpenditureKcal: -2429m, VsExpenditureDays: 2, AvgVsTargetKcal: 429m, VsTargetDays: 1, VsTargetStatus: "AboveTarget" },
    "Summary: ngày chưa đủ bị loại; chênh lệch target chỉ trung bình các ngày có target (1 ngày), expenditure 2 ngày");
Check(EnergyBalanceCalculator.Summary([], Td(0), Td(6)) is { LoggedDays: 0, AvgVsExpenditureKcal: null, VsExpenditureStatus: "Waiting", VsTargetStatus: "Waiting" } && EnergyBalanceCalculator.Relative([], Td(5), false).All(r => r is { AvgKcal: null, Status: "Waiting", LoggedDays: 0 }),
    "Không có ngày log nào: Summary và Relative đều Waiting");
Check(EnergyBalanceCalculator.Summary([new(Td(1), 1000m, 2000m, 2000m, -1000m, -1000m, false, false), new(Td(2), 1000m, 2000m, 2000m, -999m, -999m, false, false)], Td(0), Td(2)) is { AvgVsExpenditureKcal: -1000m }
    && EnergyBalanceCalculator.Summary([new(Td(0), 2000m, 2000m, 2000m, 0m, 0m, false, false), new(Td(1), 1000m, 2000m, 2000m, 1m, 1m, false, false)], Td(0), Td(1)).AvgVsExpenditureKcal == 1m
    && EnergyBalanceCalculator.Summary([new(Td(0), 1m, 2000m, 2000m, -1m, -1m, false, false), new(Td(1), 1m, 2000m, 2000m, -2m, -2m, false, false)], Td(0), Td(1)).AvgVsExpenditureKcal == -2m, "Làm tròn trung bình half away from zero (−999.5 → −1000, 0.5 → 1, −1.5 → −2)");
var ebWeekDays = EnergyBalanceCalculator.Days(Td(0), Td(13), Enumerable.Range(0, 14).ToDictionary(i => Td(i), i => Lg(i, 2000m + i * 10)), ebExp.Concat(Enumerable.Range(21, 0).Select(i => new KeyValuePair<DateOnly, decimal>(Td(i), 0m))).ToDictionary(k => k.Key, k => k.Value), _ => 2000m);
var ebBuckets = EnergyBalanceCalculator.Buckets(ebWeekDays, MetricGranularity.Week, DayOfWeek.Monday);
Check(ebBuckets.Sum(b => b.LoggedDays) == 14 && ebBuckets.All(b => b.Start.DayOfWeek == DayOfWeek.Monday && b.End == b.Start.AddDays(6)) && ebBuckets[0].AvgCaloriesKcal == Math.Round(ebWeekDays.Where(d => d.Date >= ebBuckets[0].Start && d.Date <= ebBuckets[0].End).Average(d => d.CaloriesKcal!.Value), 0, MidpointRounding.AwayFromZero)
    && EnergyBalanceCalculator.Buckets(ebWeekDays, MetricGranularity.Day, DayOfWeek.Monday).Count == 14 && EnergyBalanceCalculator.Buckets(ebWeekDays, MetricGranularity.Month, DayOfWeek.Monday).Sum(b => b.LoggedDays) == 14,
    "Buckets Day/Week/Month: trung bình các ngày đã log trong bucket");
// Target của từng ngày: tuần program hiệu lực tại ngày đó (không phải target hiện tại)
var ebProgram = new MacroProgram { Id = Guid.NewGuid(), CheckInWeekday = DayOfWeek.Monday };
ebProgram.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = Td(0), CaloriesTarget = 2000 });
ebProgram.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = Td(10), CaloriesTarget = 2200 });
var tf = EnergyBalanceSupport.TargetFn(ebProgram);
Check(tf(Td(-1)) is null && tf(Td(0)) == 2000m && tf(Td(9)) == 2000m && tf(Td(10)) == 2200m && tf(Td(30)) == 2200m && EnergyBalanceSupport.TargetFn(null)(Td(0)) is null, "Target theo ngày: tuần cũ cho ngày cũ, tuần mới từ EffectiveFrom, trước tuần đầu / không program = null");
Check(!new GetEnergyBalanceSeriesQueryValidator().Validate(new GetEnergyBalanceSeriesQuery(Td(1), Td(0))).IsValid && new GetEnergyBalanceSeriesQueryValidator().Validate(new GetEnergyBalanceSeriesQuery(null, Td(0), true, MetricGranularity.Week)).IsValid, "Validator Energy Balance: from > to bị chặn");

// --- Nutrient detail: lịch sử một chất, trung bình mỗi ngày đã log, null vs 0, target theo ngày, contributors, timing ---
FoodLogSupport.LoggedDay Nl(int d, decimal kcal, bool fasting = false, bool incomplete = false) => new(Td(d), kcal, 0, 0, 0, 0, 0, incomplete, fasting);
// Ảnh MacroFactor (Calories 15–21/10): 18: 272, 19: 100, 20: 1474, 21: 3258 → trung bình 1276 = 5104 ÷ 4 (ngày chưa log không tính)
var ndLogs = new[] { Nl(17, 272m), Nl(18, 100m), Nl(19, 1474m), Nl(20, 3258m) }.ToDictionary(l => l.Date);
var ndDays = NutrientDetailCalculator.Days(Td(14), Td(20), ndLogs, d => ndLogs[d].TotalCalories, true, d => (null, d == Td(20) ? 2880m : 2800m, null), 2);
Check(ndDays.Count == 7 && ndDays.Take(3).All(d => d.Amount is null && d.Target == 2800m) && ndDays[3] is { Amount: 272m } && ndDays[6] is { Amount: 3258m, Target: 2880m }, "Nutrient series: mọi ngày liên tục, ngày chưa log Amount null nhưng vẫn có target của ngày");
var ndSum = NutrientDetailCalculator.Summary(ndDays, Td(14), Td(20), 2);
Check(ndSum is { LoggedDays: 4, Average: 1276m, Min: 100m, Max: 3258m } && ndSum.AverageTarget == 2820m, "Header Average = trung bình mỗi ngày ĐÃ LOG (5104 ÷ 4 = 1276), không chia 7 ngày; AverageTarget = 2820 (trung bình target các ngày đã log)");
var wk7 = NutrientDetailCalculator.WindowAverage(ndDays, Td(20), 7, 2);
var mo30 = NutrientDetailCalculator.WindowAverage(ndDays, Td(20), 30, 2);
Check(wk7 == (1276m, 4) && mo30 == (1276m, 4) && NutrientDetailCalculator.WindowAverage(ndDays, Td(20), 3, 2) == (1610.67m, 3) && NutrientDetailCalculator.WindowAverage(ndDays, Td(20), 1, 2) == (3258m, 1),
    "Past Week / Past Month = trung bình ngày đã log trong 7 / 30 ngày kết thúc ở ngày đó (1276, 4 ngày); 3 ngày 1610.67; 1 ngày = hôm nay");
Check(NutrientDetailCalculator.WindowAverage(ndDays, Td(14), 7, 2) == (null, 0), "Cửa sổ không có ngày log nào: null, 0 ngày");
// null vs 0: ngày có log nhưng chất không có dữ liệu = null (không tính); ngày nhịn ăn = 0 cho macro/fiber/sodium, null cho vi chất
var ndMicro = new[] { Nl(0, 100m), Nl(1, 100m), Nl(2, 0m, fasting: true) }.ToDictionary(l => l.Date);
var mDays = NutrientDetailCalculator.Days(Td(0), Td(3), ndMicro, d => d == Td(0) ? 12.5m : null, false, _ => (null, null, null), 4);
var zDays = NutrientDetailCalculator.Days(Td(0), Td(3), ndMicro, d => d == Td(0) ? 3m : null, true, _ => (null, null, null), 2);
Check(mDays[0].Amount == 12.5m && mDays[1].Amount is null && mDays[2] is { Amount: null, IsFasting: true } && mDays[3].Amount is null && NutrientDetailCalculator.Summary(mDays, Td(0), Td(3), 4) is { LoggedDays: 1, Average: 12.5m },
    "Vi chất: ngày log không có dữ liệu = null (khác 0, không tính vào trung bình), ngày nhịn ăn null");
Check(zDays[2] is { Amount: 0m, IsFasting: true } && zDays[1].Amount is null && NutrientDetailCalculator.Summary(zDays, Td(0), Td(3), 2) is { LoggedDays: 2, Average: 1.5m } && NutrientDetailCalculator.ZeroOnFastingCodes.Contains("fiber_g") && !NutrientDetailCalculator.ZeroOnFastingCodes.Contains("vitamin_c_mg"),
    "Fiber/sodium/macro: ngày nhịn ăn = 0 và được tính (trung bình (3 + 0) ÷ 2 = 1.5)");
// ngày chưa đủ: giữ Amount + cờ nhưng không tính
var ndIncLogs = new[] { Nl(0, 2000m), Nl(1, 500m, incomplete: true), Nl(2, 3000m) }.ToDictionary(l => l.Date);
var ndIncDays = NutrientDetailCalculator.Days(Td(0), Td(2), ndIncLogs, d => ndIncLogs[d].TotalCalories, true, _ => (null, null, null), 2);
Check(ndIncDays[1] is { Amount: 500m, IsIncomplete: true } && NutrientDetailCalculator.Summary(ndIncDays, Td(0), Td(2), 2) is { LoggedDays: 2, Average: 2500m, Min: 2000m } && NutrientDetailCalculator.WindowAverage(ndIncDays, Td(2), 7, 2) == (2500m, 2),
    "Ngày chưa đủ: vẫn có Amount nhưng bị loại khỏi trung bình / min / max (cùng quy tắc Energy Balance)");
Check(NutrientDetailCalculator.Days(Td(0), Td(1), ndLogs, _ => 1m, true, _ => (null, null, null), 2).All(d => d.Amount is null) && NutrientDetailCalculator.Summary([], Td(0), Td(1), 2) is { LoggedDays: 0, Average: null, Min: null }, "Không có ngày log: Amount null hết, Summary rỗng");
// Floor/ceiling (Range) và target mỗi ngày
var rangeDays = NutrientDetailCalculator.Days(Td(0), Td(1), ndIncLogs, d => 10m, false, _ => (8m, 18m, 40m), 4);
Check(rangeDays.All(d => d is { Floor: 8m, Target: 18m, Ceiling: 40m }), "Vi chất: Range floor→ceiling và target áp cho mọi ngày");
var ndBuckets = NutrientDetailCalculator.Buckets(NutrientDetailCalculator.Days(Td(0), Td(13), Enumerable.Range(0, 14).ToDictionary(i => Td(i), i => Nl(i, 1000m + i * 100)), d => 1000m + (d.DayNumber - Td(0).DayNumber) * 100, true, d => (null, 2000m, null), 2), MetricGranularity.Week, DayOfWeek.Monday, 2);
Check(ndBuckets.Sum(b => b.LoggedDays) == 14 && ndBuckets.All(b => b.Start.DayOfWeek == DayOfWeek.Monday && b.End == b.Start.AddDays(6) && b.AverageTarget == 2000m) && NutrientDetailCalculator.Buckets(ndDays, MetricGranularity.Day, DayOfWeek.Monday, 2).Count == 4,
    "Buckets Week (thứ Hai) / Day: trung bình ngày đã log, chỉ bucket có ngày log");
// Contributors: giảm dần, tên làm thứ tự phụ, bỏ 0 và null
var cIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
var contribs = NutrientDetailCalculator.Contributors([(cIds[0], "Rice", 200m), (cIds[1], "Egg", 300.126m), (cIds[2], "Apple", 200m), (cIds[3], "Water", 0m), (cIds[4], "Unknown", null)], 2);
Check(contribs.Select(c => c.Name).SequenceEqual(["Egg", "Apple", "Rice"]) && contribs[0] is { Amount: 300.13m } && contribs[0].EntryId == cIds[1] && NutrientDetailCalculator.Contributors([], 2).Count == 0,
    "Contributors: lượng giảm dần, bằng nhau thì theo tên, bỏ entry 0 / không có dữ liệu, làm tròn");
// Timing: 24 khung giờ
var timing = NutrientDetailCalculator.Timing([(new TimeOnly(7, 5), 300m), (new TimeOnly(7, 59), 100m), (new TimeOnly(12, 0), 500m), (new TimeOnly(23, 59), 50m), (new TimeOnly(0, 0), 10m), (new TimeOnly(9, 0), null), (new TimeOnly(9, 30), 0m)], 2);
Check(timing.Count == 24 && timing.Select(t => t.Hour).SequenceEqual(Enumerable.Range(0, 24)) && timing[7].Amount == 400m && timing[12].Amount == 500m && timing[23].Amount == 50m && timing[0].Amount == 10m && timing[9].Amount == 0m && timing.Sum(t => t.Amount) == 960m
    && NutrientDetailCalculator.Timing([], 2).All(t => t.Amount == 0m), "Timing: luôn 24 khung giờ, cộng theo giờ ăn, entry không có dữ liệu bị bỏ; rỗng thì toàn 0");
Check(NutrientDetailCalculator.TargetSource("program") == "Strategy" && NutrientDetailCalculator.TargetSource("user") == "Custom" && NutrientDetailCalculator.TargetSource("dri") == "Default" && NutrientDetailCalculator.TargetSource("reference") == "Default"
    && NutrientDetailCalculator.TargetSource("default") == "Default" && NutrientDetailCalculator.TargetSource("none") == "None", "Nguồn mục tiêu: program → Strategy, user → Custom, dri/reference/default → Default, none → None");
Check(NutrientDetailCalculator.Decimals("energy_kcal") == 2 && NutrientDetailCalculator.Decimals("fiber_g") == 2 && NutrientDetailCalculator.Decimals("vitamin_b12_mcg") == 4, "Số lẻ: macro/fiber/sodium 2, vi chất 4");
Check(!new GetNutrientSeriesQueryValidator().Validate(new GetNutrientSeriesQuery("energy_kcal", Td(1), Td(0))).IsValid && !new GetNutrientSeriesQueryValidator().Validate(new GetNutrientSeriesQuery("", null, null)).IsValid && new GetNutrientSeriesQueryValidator().Validate(new GetNutrientSeriesQuery("fiber_g", null, Td(0), true, MetricGranularity.Month)).IsValid
    && !new GetNutrientDayDetailQueryValidator().Validate(new GetNutrientDayDetailQuery("", null)).IsValid && new GetNutrientDayDetailQueryValidator().Validate(new GetNutrientDayDetailQuery("energy_kcal", Td(0))).IsValid, "Validator nutrient detail: mã rỗng và from > to bị chặn");
var tfp = new MacroProgram { Id = Guid.NewGuid(), CheckInWeekday = DayOfWeek.Monday };
tfp.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = Td(0), CaloriesTarget = 2000, ProteinTarget = 150, CarbsTarget = 200, FatTarget = 60 });
tfp.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = Td(10), CaloriesTarget = 2200, ProteinTarget = 170, CarbsTarget = 220, FatTarget = 70 });
var pf = EnergyBalanceSupport.TargetFn(tfp, w => w.ProteinTarget); var ff = EnergyBalanceSupport.TargetFn(tfp, w => w.FatTarget);
Check(pf(Td(5)) == 150m && pf(Td(12)) == 170m && ff(Td(5)) == 60m && ff(Td(12)) == 70m && pf(Td(-1)) is null && EnergyBalanceSupport.TargetFn(tfp)(Td(12)) == 2200m, "Target macro theo ngày: protein/fat/carbs đổi theo tuần program hiệu lực (như calories)");

// --- Nutrient goals: Floor/Target/Ceiling + goalKind (seed → DRI chỉ trường DRI định nghĩa → user nguyên bộ) ---
// RDA/AI là Target, KHÔNG phải Floor (không có số LTI thật): mọi chất RDA có Floor null
var rdaCodes = new[] { "vitamin_a_mcg", "vitamin_c_mg", "vitamin_d_mcg", "vitamin_e_mg", "vitamin_k_mcg", "vitamin_b1_mg", "vitamin_b2_mg", "vitamin_b3_mg", "vitamin_b5_mg", "vitamin_b6_mg", "vitamin_b12_mcg", "folate_mcg", "choline_mg",
    "calcium_mg", "copper_mg", "iron_mg", "magnesium_mg", "manganese_mg", "phosphorus_mg", "potassium_mg", "selenium_mcg", "zinc_mg", "omega3_ala_g", "omega6_g", "water_g" };
Check(rdaCodes.All(c => Dri(c, Gender.Male, 30) is { Floor: null, Target: not null }) && rdaCodes.All(c => Dri(c, Gender.Female, null) is { Floor: null, Target: not null })
    && Dri("fiber_g", Gender.Female, 30, 2000) is { Floor: null, Target: 28m }, "DRI: mọi chất RDA/AI có Floor null (Target = RDA, Ceiling = UL nếu có); fiber 14 g/1000 kcal là Target");
NutrientGoal Goal(decimal? sf, decimal? st, decimal? sc, string code, Gender g = Gender.Male, int? age = 30, decimal? kcal = null, UserGoal? user = null)
{ var d = DriTable.Resolve(code, g, age, kcal, out var isRef); return NutrientGoals.Resolve(sf, st, sc, d, isRef, user); }
// Loại mục tiêu
Check(NutrientGoals.KindOf(8m, null, 40m, "default") == "Range" && NutrientGoals.KindOf(8m, 18m, 40m, "user") == "Range" && NutrientGoals.KindOf(null, 90m, 2000m, "dri") == "TargetWithCeiling" && NutrientGoals.KindOf(null, null, 22m, "default") == "Maximum"
    && NutrientGoals.KindOf(8m, null, null, "user") == "Minimum" && NutrientGoals.KindOf(8m, 18m, null, "user") == "Minimum" && NutrientGoals.KindOf(null, 2.4m, null, "dri") == "Minimum" && NutrientGoals.KindOf(null, 2.4m, null, "reference") == "Minimum" && NutrientGoals.KindOf(null, 2.4m, null, "default") == "Minimum"
    && NutrientGoals.KindOf(null, 2880m, null, "program") == "Target" && NutrientGoals.KindOf(null, 100m, null, "user") == "Target" && NutrientGoals.KindOf(null, null, null, "none") == "None",
    "goalKind: Range (floor + ceiling), TargetWithCeiling, Maximum, Minimum (floor / target RDA không trần), Target (program / user một mức), None");
// Natri = Target + Ceiling; vitamin C = Target + UL; macro = Target; chất ceiling-only = Maximum; RDA = Minimum
var sodium = Goal(null, null, 2300m, "sodium_mg");
var vitC = Goal(null, 90m, null, "vitamin_c_mg");
Check(sodium is { Floor: null, Target: 1500m, Ceiling: 2300m, Kind: "TargetWithCeiling", Basis: "dri" } && vitC is { Floor: null, Target: 90m, Ceiling: 2000m, Kind: "TargetWithCeiling" } && Goal(null, 1.6m, null, "omega3_ala_g").Kind == "Minimum"
    && Goal(null, null, 50m, "sugars_added_g", kcal: 2000).Kind == "Maximum" && Goal(null, null, 22m, "fat_saturated_g", kcal: 2000) is { Ceiling: 22.2m, Kind: "Maximum" } && Goal(null, null, null, "caffeine_mg") is { Ceiling: 400m, Kind: "Maximum" },
    "Natri 1500/2300 và vitamin C 90/2000 = TargetWithCeiling; béo bão hoà (10% kcal DGA), đường thêm, caffeine = Maximum; RDA không trần = Minimum");
// DRI None KHÔNG xoá seed: trans giữ trần 2 g; chất không seed + None vẫn None
var seededNone = Goal(null, null, 300m, "cholesterol_mg");
Check(seededNone is { Floor: null, Target: null, Ceiling: 300m, Kind: "Maximum", Basis: "default" } && Goal(null, null, null, "omega3_epa_g").Kind == "None" && Goal(null, null, null, "leucine_g") is { Kind: "None", Basis: "default" }
    && Goal(null, null, 2m, "no_dri_code").Ceiling == 2m, "DRI None không xoá seed (chất DRI không đặt mức nhưng có seed giữ trần seed, Maximum); chất không seed + DRI None = None; chất ngoài bảng DRI dùng seed");
// Béo trans: WHO < 1% năng lượng → trần = 1% kcal ÷ 9 (g); chưa có program = 2000 kcal tham chiếu (2.2 g); thắng seed cũ 2 g còn trong DB
Check(Dri("fat_trans_g", Gender.Male, 30, 2000) is { Floor: null, Target: null, Ceiling: 2.2m } && Dri("fat_trans_g", Gender.Female, 30, 2400) is { Ceiling: 2.7m } && Dri("fat_trans_g", Gender.Male, 30, 1800) is { Ceiling: 2m }
    && Dri("fat_trans_g", Gender.Male, 30) is { Ceiling: 2.2m } && Dri("fat_trans_g", Gender.Other, null, 0) is { Ceiling: 2.2m } && Goal(null, null, 2m, "fat_trans_g", kcal: 3000) is { Ceiling: 3.3m, Kind: "Maximum", Basis: "dri" } && Goal(null, null, null, "fat_trans_g") is { Ceiling: 2.2m, Kind: "Maximum" },
    "Béo trans: trần WHO < 1% kcal ÷ 9 g (2000 kcal = 2.2, 2400 = 2.7, 1800 = 2.0); chưa có kcal dùng 2000 tham chiếu; DRI thắng seed cũ 2 g; Maximum");
// Trộn theo trường: DRI chỉ ghi đè trường nó định nghĩa; seed lấp trường còn thiếu; sai thứ tự thì lấy riêng DRI
Check(NutrientGoals.Resolve(null, 30m, 50m, new DriRange(null, 33.6m, null), false, null) is { Target: 33.6m, Ceiling: 50m, Floor: null }
    && NutrientGoals.Resolve(null, 50m, null, new DriRange(null, null, 40m), false, null) is { Floor: null, Target: null, Ceiling: 40m, Kind: "Maximum" }
    && NutrientGoals.Resolve(5m, 10m, 20m, null, false, null) is { Floor: 5m, Target: 10m, Ceiling: 20m, Kind: "Range", Basis: "default" } && NutrientGoals.IsOrdered(null, 5m, 5m) && !NutrientGoals.IsOrdered(6m, 5m, null) && !NutrientGoals.IsOrdered(null, 9m, 5m),
    "Trộn: DRI chỉ ghi đè trường nó có (seed lấp phần còn lại), trộn sai thứ tự thì lấy riêng DRI; seed nguyên khi không có DRI");
// Ghi đè của user: NGUYÊN BỘ, không trộn từng trường; cả ba null = về mặc định
var customTarget = Goal(null, 90m, null, "vitamin_c_mg", user: new UserGoal(null, 100m, null));
var customRange = Goal(null, 90m, null, "vitamin_c_mg", user: new UserGoal(20m, 80m, 500m));
var customFloor = Goal(null, null, 2300m, "sodium_mg", user: new UserGoal(500m, null, null));
Check(customTarget is { Floor: null, Target: 100m, Ceiling: null, Kind: "Target", Basis: "user" } && customRange is { Floor: 20m, Target: 80m, Ceiling: 500m, Kind: "Range", Basis: "user" } && customFloor is { Floor: 500m, Target: null, Ceiling: null, Kind: "Minimum", Basis: "user" },
    "User ghi đè nguyên bộ: chỉ đặt target 100 thì KHÔNG thừa kế trần UL 2000 / floor; floor + target + ceiling thành Range; chỉ floor = Minimum (không thừa kế trần mặc định)");
Check(Goal(null, 90m, null, "vitamin_c_mg", user: new UserGoal(null, null, null)) is { Target: 90m, Ceiling: 2000m, Basis: "dri", Kind: "TargetWithCeiling" } && new UserGoal(null, null, null).IsEmpty && !new UserGoal(null, 1m, null).IsEmpty,
    "Cả ba null = về mặc định (DRI)");
// Calories/P/F/C: Target theo program (không floor/ceiling); chưa có program thì None
Check(NutrientGoals.Make(null, 2880m, null, "program") is { Kind: "Target", Floor: null, Ceiling: null } && NutrientGoals.Make(null, null, null, "program").Kind == "None", "Macro: Target từ program, chưa có program = None");
// Validator của lệnh đặt target: thứ tự Floor ≤ Target ≤ Ceiling, âm bị chặn, cả ba null hợp lệ
var utv = new UpdateNutrientTargetCommandValidator();
Check(utv.Validate(new UpdateNutrientTargetCommand("vitamin_c_mg", 20m, 80m, 500m)).IsValid && utv.Validate(new UpdateNutrientTargetCommand("vitamin_c_mg", null, 100m, null)).IsValid && utv.Validate(new UpdateNutrientTargetCommand("vitamin_c_mg", null, null, null)).IsValid
    && utv.Validate(new UpdateNutrientTargetCommand("vitamin_c_mg", 10m, null, 10m)).IsValid && !utv.Validate(new UpdateNutrientTargetCommand("vitamin_c_mg", 90m, 80m, null)).IsValid && !utv.Validate(new UpdateNutrientTargetCommand("vitamin_c_mg", null, 100m, 50m)).IsValid
    && !utv.Validate(new UpdateNutrientTargetCommand("vitamin_c_mg", 60m, null, 50m)).IsValid && !utv.Validate(new UpdateNutrientTargetCommand("vitamin_c_mg", -1m, null, null)).IsValid, "Đặt target: thứ tự Floor ≤ Target ≤ Ceiling (từng cặp có mặt), không âm; cả ba null = về mặc định");

// --- Period theo NGÀY: bật/tắt một ngày trên nền khoảng (mở rộng, gộp, thu hẹp, tách, xoá, idempotent) ---
PeriodRange Pr(int s, int? e, string? sym = null, string? notes = null) => new(Guid.NewGuid(), Td(s), e is { } ee ? Td(ee) : null, sym, notes);
var prToday = Td(20);
string Show(IReadOnlyList<PeriodRange> rs) => string.Join(" ", rs.Select(r => $"[{r.Start.DayNumber - Td(0).DayNumber},{(r.End is { } e ? (e.DayNumber - Td(0).DayNumber).ToString() : "open")}]"));
var pA = Pr(1, 3, "sa", "a");
var ext = PeriodDays.SetDay([pA], Td(4), true, prToday);
Check(ext.Changed && ext.Ranges.Count == 1 && ext.Ranges[0] == pA with { End = Td(4) }, $"Period: bật ngày sát cuối kỳ → kéo dài, giữ Id/triệu chứng ({Show(ext.Ranges)})");
var extStart = PeriodDays.SetDay([pA], Td(0), true, prToday);
Check(extStart.Changed && extStart.Ranges.Count == 1 && extStart.Ranges[0] == pA with { Start = Td(0) }, $"Period: bật ngày sát đầu kỳ → kéo lùi đầu ({Show(extStart.Ranges)})");
var created = PeriodDays.SetDay([pA], Td(10), true, prToday);
Check(created.Changed && created.Ranges.Count == 2 && created.Ranges[1] is { Start: var cs, End: var ce, Symptoms: null, Notes: null } && cs == Td(10) && ce == Td(10) && created.Ranges[1].Id != pA.Id && created.Ranges[0] == pA, "Period: bật ngày xa mọi kỳ → tạo kỳ mới [ngày, ngày] (kỳ cũ giữ nguyên)");
var gapA = Pr(1, 3, "sa", "a"); var gapB = Pr(5, 7, "sb", "b");
var merged = PeriodDays.SetDay([gapB, gapA], Td(4), true, prToday);
Check(merged.Changed && merged.Ranges.Count == 1 && merged.Ranges[0] is { Symptoms: "sa", Notes: "a\nb" } && merged.Ranges[0].Id == gapA.Id && merged.Ranges[0].Start == Td(1) && merged.Ranges[0].End == Td(7), $"Period: lấp khe 1 ngày → gộp, giữ Id + triệu chứng kỳ sớm, nối ghi chú kỳ sau ({Show(merged.Ranges)})");
var gapOpen = Pr(5, null, null, "b");
var mergedOpen = PeriodDays.SetDay([Pr(1, 3, null, "a"), gapOpen], Td(4), true, prToday);
Check(mergedOpen.Ranges.Count == 1 && mergedOpen.Ranges[0].End is null && mergedOpen.Ranges[0].Notes == "a\nb" && PeriodDays.SetDay([Pr(1, 3, null, null), Pr(5, 7, null, null)], Td(4), true, prToday).Ranges[0].Notes is null, "Period: gộp với kỳ sau đang diễn ra → kỳ gộp vẫn đang diễn ra; không ghi chú thì null");
var longA = Pr(1, 3, null, new string('x', 400)); var longB = Pr(5, 7, null, new string('y', 400));
Check(PeriodDays.SetDay([longA, longB], Td(4), true, prToday).Ranges[0].Notes!.Length == PeriodDays.NotesMaxLength, "Period: ghi chú nối vượt 500 ký tự bị cắt về 500");
var idem1 = PeriodDays.SetDay([pA], Td(2), true, prToday);
var idem2 = PeriodDays.SetDay([pA], Td(10), false, prToday);
Check(!idem1.Changed && idem1.Ranges.Single() == pA && !idem2.Changed && idem2.Ranges.Single() == pA && !PeriodDays.SetDay([], Td(3), false, prToday).Changed, "Period: idempotent — bật ngày đã theo dõi / tắt ngày chưa theo dõi / tắt khi không có kỳ nào = không đổi");
var single = Pr(5, 5);
Check(PeriodDays.SetDay([single], Td(5), false, prToday) is { Changed: true, Ranges.Count: 0 }, "Period: tắt kỳ 1 ngày → xoá kỳ");
var offStart = PeriodDays.SetDay([pA], Td(1), false, prToday); var offEnd = PeriodDays.SetDay([pA], Td(3), false, prToday);
Check(offStart.Ranges.Single() == pA with { Start = Td(2) } && offEnd.Ranges.Single() == pA with { End = Td(2) } && offStart.Changed && offEnd.Changed, "Period: tắt ngày đầu → thu hẹp đầu; tắt ngày cuối → thu hẹp cuối");
var split = PeriodDays.SetDay([Pr(1, 5, "sym", "note")], Td(3), false, prToday);
Check(split.Ranges.Count == 2 && split.Ranges[0] is { Notes: "note", Symptoms: "sym" } && split.Ranges[0].End == Td(2) && split.Ranges[1].Start == Td(4) && split.Ranges[1].End == Td(5) && split.Ranges[1] is { Symptoms: "sym", Notes: null } && split.Ranges[1].Id != split.Ranges[0].Id,
    $"Period: tắt ngày giữa → tách hai; kỳ sau có Id mới, sao triệu chứng ({Show(split.Ranges)})");
var splitId = Pr(1, 5); var splitKeep = PeriodDays.SetDay([splitId], Td(3), false, prToday);
Check(splitKeep.Ranges[0].Id == splitId.Id, "Period: tách giữ Id gốc cho kỳ sớm");
// Kỳ đang diễn ra (End null, tính tới hôm nay)
var open = Pr(18, null, "s", "n");
Check(PeriodDays.Tracks(open, Td(20), prToday) && !PeriodDays.Tracks(open, Td(21), prToday) && PeriodDays.LengthDays(open, prToday) == 3 && PeriodDays.LengthDays(pA, prToday) == 3, "Period: kỳ đang diễn ra tính tới hôm nay (độ dài 3 ngày), kỳ đóng theo hai đầu");
var openMid = PeriodDays.SetDay([open], Td(19), false, prToday);
Check(openMid.Ranges.Count == 2 && openMid.Ranges[0].End == Td(18) && openMid.Ranges[1].Start == Td(20) && openMid.Ranges[1].End is null && openMid.Ranges[1].Symptoms == "s", $"Period: tắt ngày giữa kỳ đang diễn ra → kỳ sau vẫn đang diễn ra ({Show(openMid.Ranges)})");
var openToday = PeriodDays.SetDay([open], Td(20), false, prToday);
Check(openToday.Ranges.Single().End == Td(19) && PeriodDays.SetDay([Pr(20, null)], Td(20), false, prToday) is { Changed: true, Ranges.Count: 0 }, "Period: tắt hôm nay của kỳ đang diễn ra → đóng kỳ hôm qua; kỳ chỉ có hôm nay → xoá");
Check(PeriodDays.SetDay([Pr(10, 12), Pr(15, 20, null, null)], Td(13), true, prToday).Ranges.Select(r => (r.Start.DayNumber - Td(0).DayNumber, (r.End?.DayNumber ?? 0) - Td(0).DayNumber)).SequenceEqual([(10, 13), (15, 20)])
    && PeriodDays.SetDay([Pr(10, 12), Pr(14, 20)], Td(13), true, prToday).Ranges.Count == 1, "Period: bật ngày sát cuối kỳ A nhưng cách kỳ B 1 ngày → kéo dài; lấp khe thật sự mới gộp");
// Trải ngày
var exp = PeriodDays.Expand([Pr(1, 3), Pr(2, 4), Pr(8, null)], Td(2), Td(20), prToday);
Check(exp.Select(d => d.DayNumber - Td(0).DayNumber).SequenceEqual([2, 3, 4, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20]) && PeriodDays.Expand([Pr(8, null)], Td(0), Td(30), prToday).Max() == prToday && PeriodDays.Expand([], Td(0), Td(5), prToday).Count == 0 && PeriodDays.Expand([Pr(1, 3)], Td(5), Td(9), prToday).Count == 0,
    "Period: trải ngày cắt theo khoảng, không trùng, kỳ bắt đầu trước from vẫn có mặt, kỳ đang diễn ra dừng ở hôm nay");
// Tính lại độ dài chu kỳ sau khi tách / gộp
var recompute = new List<PeriodLog>
{
    new() { Id = Guid.NewGuid(), StartDate = Td(1), EndDate = Td(2) }, new() { Id = Guid.NewGuid(), StartDate = Td(4), EndDate = Td(4) }, new() { Id = Guid.NewGuid(), StartDate = Td(30), EndDate = null },
};
PeriodSupport.Recompute(recompute);
Check(recompute[0].CycleLengthDays is null && recompute[0].PeriodLengthDays == 2 && recompute[1].CycleLengthDays == 3 && recompute[1].PeriodLengthDays == 1 && recompute[2].CycleLengthDays == 26 && recompute[2].PeriodLengthDays is null, "Period: cycleLength = khoảng cách StartDate kỳ trước, periodLength gồm hai đầu (kỳ đang diễn ra null)");
var pgv = new GetPeriodLogsQueryValidator();
Check(pgv.Validate(new GetPeriodLogsQuery(null, null)).IsValid && pgv.Validate(new GetPeriodLogsQuery(Td(0), Td(0).AddDays(3659))).IsValid && !pgv.Validate(new GetPeriodLogsQuery(Td(0), Td(0).AddDays(3660))).IsValid && !pgv.Validate(new GetPeriodLogsQuery(Td(5), Td(0))).IsValid,
    "GET /periods: khoảng tối đa 3660 ngày (3660 hợp lệ, 3661 bị chặn), from > to bị chặn");
Check(!new SetPeriodDayCommandValidator().Validate(new SetPeriodDayCommand(Td(0), null)).IsValid && new SetPeriodDayCommandValidator().Validate(new SetPeriodDayCommand(Td(0), true)).IsValid && new SetPeriodDayCommandValidator().Validate(new SetPeriodDayCommand(null, false)).IsValid, "PUT days/{date}: isOnPeriod bắt buộc; ngày lấy từ route (nullable trong body)");

// all=true: chuỗi Calories/P/F/C bắt đầu từ ngày sớm hơn giữa log đầu tiên và tuần program đầu tiên; vi chất (không có ngày target riêng) từ log đầu tiên; không vượt 3660 ngày
Check(NutrientDetailSupport.FirstDay(Td(10), Td(0), Td(20)) == Td(0) && NutrientDetailSupport.FirstDay(Td(0), Td(10), Td(20)) == Td(0) && NutrientDetailSupport.FirstDay(Td(10), null, Td(20)) == Td(10) && NutrientDetailSupport.FirstDay(null, Td(5), Td(20)) == Td(5)
    && NutrientDetailSupport.FirstDay(null, null, Td(20)) == Td(20) && NutrientDetailSupport.FirstDay(Td(10), Td(25), Td(20)) == Td(10) && NutrientDetailSupport.FirstDay(null, Td(25), Td(20)) == Td(20),
    "Series all=true: from = min(log đầu, tuần program đầu); micro (không có ngày target) từ log đầu; chưa có gì = hôm nay; tuần program ở tương lai bị bỏ qua");
Check(NutrientDetailSupport.FirstDay(Td(-9000), null, Td(20)) == Td(20).AddDays(-3659) && NutrientDetailSupport.FirstDay(Td(-9000), Td(-9500), Td(20)) == Td(20).AddDays(-3659) && Td(20).DayNumber - NutrientDetailSupport.FirstDay(Td(-9000), null, Td(20)).DayNumber + 1 == NutrientDetailSupport.MaxRangeDays,
    "Series all=true: chuỗi tối đa 3660 ngày (lùi từ hôm nay, không 400)");

// --- Lịch sử mục tiêu tuỳ chỉnh: ngày d dùng lần đổi gần nhất có hiệu lực ≤ d; reset về mặc định cũng là một lần đổi ---
GoalChange Gc(int d, decimal? f, decimal? t, decimal? c) => new(Td(d), f, t, c);
var hist = new List<GoalChange> { Gc(5, 20, 80, 500), Gc(10, null, null, null), Gc(15, null, 100, null) };
Check(NutrientGoalHistory.On(hist, Td(4)) is null && NutrientGoalHistory.On(hist, Td(5)) == new UserGoal(20, 80, 500) && NutrientGoalHistory.On(hist, Td(9)) == new UserGoal(20, 80, 500)
    && NutrientGoalHistory.On(hist, Td(10)) is null && NutrientGoalHistory.On(hist, Td(14)) is null && NutrientGoalHistory.On(hist, Td(15)) == new UserGoal(null, 100, null) && NutrientGoalHistory.On(hist, Td(99)) == new UserGoal(null, 100, null),
    "Lịch sử goal: trước lần đổi đầu = mặc định; đổi giữa kỳ chỉ áp từ ngày hiệu lực; reset (dòng rỗng) từ ngày X = mặc định; lần đổi sau thay lần trước");
Check(NutrientGoalHistory.On([], Td(3)) is null && NutrientGoalHistory.On([Gc(7, null, null, null)], Td(7)) is null, "Lịch sử goal: không có lần đổi / chỉ có reset = mặc định");
// Ngày cũ giữ goal cũ khi đổi goal: mô phỏng chuỗi mỗi ngày với mức mặc định vitamin C (90 / UL 2000) và goal tuỳ chỉnh đổi giữa kỳ
var vcDri = DriTable.Resolve("vitamin_c_mg", Gender.Male, 30, null, out var vcRef);
NutrientGoal VcOn(int day) => NutrientGoals.Resolve(null, 90m, null, vcDri, vcRef, NutrientGoalHistory.On(hist, Td(day)));
Check(VcOn(3) is { Target: 90m, Ceiling: 2000m, Kind: "TargetWithCeiling", Basis: "dri" } && VcOn(6) is { Floor: 20m, Target: 80m, Ceiling: 500m, Kind: "Range", Basis: "user" } && VcOn(11) is { Target: 90m, Ceiling: 2000m, Kind: "TargetWithCeiling" }
    && VcOn(16) is { Floor: null, Target: 100m, Ceiling: null, Kind: "Target", Basis: "user" },
    "Goal theo ngày: ngày −5.. giữ 20/80/500 (Range), từ ngày 10 reset về DRI 90/2000, từ 15 chỉ target 100 (không thừa kế trần UL) — ngày cũ không đổi");
// Mỗi kind từ goal hiệu lực + consumed = 0 / null (ngày chưa log): Amount là số hoặc null, floor/target/ceiling null khi không có mức
var kindDays = new[] { NutrientGoals.Make(8, null, 40, "user"), NutrientGoals.Make(null, 90, 2000, "dri"), NutrientGoals.Make(null, null, 22, "default"), NutrientGoals.Make(8, null, null, "user"), NutrientGoals.Make(null, 2.4m, null, "dri"), NutrientGoals.Make(null, 2880, null, "program"), NutrientGoals.Make(null, null, null, "none") };
Check(kindDays.Select(g => g.Kind).SequenceEqual(["Range", "TargetWithCeiling", "Maximum", "Minimum", "Minimum", "Target", "None"]), "Mỗi loại goal (Range, TargetWithCeiling, Maximum, Minimum ×2, Target, None) suy đúng từ mức hiệu lực");
var zeroDay = NutrientDetailCalculator.Days(Td(0), Td(2), new Dictionary<DateOnly, FoodLogSupport.LoggedDay> { [Td(1)] = new(Td(1), 0m, 0, 0, 0, 0, 0) }, d => 0m, true, d => NutrientGoalHistory.On([], d) is { } u ? (u.Floor, u.Target, u.Ceiling) : (null, null, null), 2);
Check(zeroDay[0].Amount is null && zeroDay[1].Amount == 0m && zeroDay[2].Amount is null && zeroDay.All(d => d is { Floor: null, Target: null, Ceiling: null }), "Consumed: ngày chưa log null, ngày log 0 kcal = 0 (khác null); không goal tuỳ chỉnh → floor/target/ceiling null");

// --- ReferenceCache: cache tham chiếu của đường ghi (mã chất, món hệ thống) ---
var cacheClock = new StepClock();
var refCache = new ReferenceCache(cacheClock);
refCache.Set("k", new[] { 1, 2 }, TimeSpan.FromMinutes(5));
var hit1 = refCache.TryGet<int[]>("k", out var cached1);
cacheClock.Now = cacheClock.Now.AddMinutes(6);
Check(hit1 && cached1.Length == 2 && !refCache.TryGet<int[]>("k", out _) && !refCache.TryGet<string>("k", out _) && !refCache.TryGet<int[]>("missing", out _),
    "ReferenceCache: trả giá trị trước hạn, hết TTL / sai kiểu / thiếu khoá thì miss");

Console.WriteLine(Environment.ExitCode == 0 ? "ALL PASS (incl. ALG-02, ALG-03, ALG-04, MOS-005, Dashboard S9, Expenditure flux, AI Plate)" : "SOME FAIL");

sealed record BadBindProbe([property: System.Text.Json.Serialization.JsonIgnore] string Key, int Value);
sealed record GoodBindProbe([property: System.Text.Json.Serialization.JsonIgnore] string? Key, int Value);

sealed class StepClock : TimeProvider
{
    public DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
}

sealed class StubAnalyzer : IAiMealAnalyzer
{
    public Task<AiMealDraft> AnalyzeAsync(IReadOnlyList<MealPhoto> photos, string? description, IReadOnlyList<string> canonicalNames, CancellationToken ct)
        => Task.FromResult(new AiMealDraft([new AiDraftDish("Egg dish", [new AiDraftIngredient("Egg", 50)])], "low"));

    public Task<AiIngredientEstimate> EstimateIngredientAsync(string name, decimal grams, CancellationToken ct)
        => Task.FromResult(new AiIngredientEstimate(70, 6, 5, 0.5m));
}

sealed class StubNames : IAiCanonicalNames
{
    public Task<IReadOnlyList<string>> GetAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>(["Egg", "Vegetable Oil"]);
}

// Giả rỗng cho check không cần DB: ghi nhận Add, SaveChanges = 1 dòng
class FakeUow : IUnitOfWork
{
    public readonly List<object> Added = [];
    public IDerivedDataScheduler Derived => throw new NotSupportedException();
    public IReferenceCache Cache => throw new NotSupportedException();
    public IGenericRepository<T> Repository<T>() where T : EntityBase<Guid> => new FakeRepo<T>(Added);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken ct = default) => Task.CompletedTask;
    public Task CommitTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Dispose() { }
}

class FakeRepo<T>(List<object> added) : IGenericRepository<T> where T : EntityBase<Guid>
{
    public Task<T> AddAsync(T entity, CancellationToken ct = default) { added.Add(entity); return Task.FromResult(entity); }
    public Task RemoveAsync(T entity, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) => Task.CompletedTask;
    public Task<T?> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Task.FromResult<T?>(null);
    public Task<T?> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default, params System.Linq.Expressions.Expression<Func<T, object>>[] includes) => Task.FromResult<T?>(null);
    public Task<T?> FindTrackedAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Task.FromResult<T?>(null);
    public Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Task.FromResult(false);
    public Task<IReadOnlyList<T>> GetAllAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<T>>([]);
    public Task<IReadOnlyList<T>> GetAllTrackedAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<T>>([]);
    public Task<QueryResult<T>> GetPagedAsync(QueryInfo query, System.Linq.Expressions.Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, CancellationToken ct = default) => throw new NotSupportedException();
    public IQueryable<T> Query(bool tracking = false) => throw new NotSupportedException();
}

class FakeUser : IUserContext
{
    public Guid UserId { get; } = Guid.NewGuid();
    public string Email => "t@t";
    public string Role => "User";
    public bool IsAuthenticated => true;
}
