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
try { await new AnalyzeMealCommandHandler(new StubAnalyzer(), aiOpts, fakeUow, new FakeUser(), TimeProvider.System).Handle(new AnalyzeMealCommand([], "2 eggs"), default); Check(false, "AI: thiếu khoá phải ném 503"); }
catch (ServiceUnavailableException e) { Check(e.StatusCode == 503 && e.Message == "AI chưa được cấu hình", "AI: thiếu khoá → 503 'AI chưa được cấu hình'"); }
var aiWithKey = await new AnalyzeMealCommandHandler(new StubAnalyzer(), Microsoft.Extensions.Options.Options.Create(new AiOptions { ApiKey = "k" }), fakeUow, new FakeUser(), TimeProvider.System).Handle(new AnalyzeMealCommand([], "2 eggs"), default);
Check(aiWithKey.Dishes.Count == 1 && aiWithKey.Dishes[0].Ingredients[0].Name == "Egg", "AI: có khoá → gọi analyzer và chuẩn hoá");
var savedScan = fakeUow.Added.OfType<FoodScanAnalysis>().SingleOrDefault();
Check(savedScan is not null && aiWithKey.ScanId == savedScan.Id && savedScan.Status == ScanStatus.Completed && savedScan.Mode == ScanMode.Describe && savedScan.ResultJson!.Contains("Egg") && savedScan.StorageRef is null, "AI scan: lưu kết quả (Completed, không lưu ảnh) và trả ScanId");

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
Check(NutritionCalculator.NetCarbs(10, 15) == 0 && NutritionCalculator.NetCarbs(10, null) == 10 && NutritionCalculator.NetCarbs(30, 5) == 25, "Engine: net carbs không âm, thiếu fiber coi như 0");
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
Check(FoodIconKeys.All.Count == 300 && FoodIconKeys.IsValid("apple") && FoodIconKeys.IsValid("brown_rice") && !FoodIconKeys.IsValid("Apple") && !FoodIconKeys.IsValid("bread") && !FoodIconKeys.IsValid(null) && !FoodIconKeys.IsValid(""), "Icon: 300 khoá hợp lệ, phân biệt hoa thường, không có bread");
var badIconKeys = IconKeyResolver.AllKeys.Where(k => !FoodIconKeys.IsValid(k)).ToList();
Check(badIconKeys.Count == 0, $"Icon: mọi khoá bảng từ khoá trả ra đều có trong bộ icon ({string.Join(",", badIconKeys)})");
(string Name, FoodCategory? Cat, string? Expect)[] iconCases =
[
    ("Apple, Fresh, With Skin", null, "apple"), ("Táo", FoodCategory.Fruits, "apple"), ("Gà", null, "chicken"), ("Cơm trắng", FoodCategory.Grains, "rice"),
    ("Trứng gà luộc", FoodCategory.Protein, "eggs"), ("Cá basa", FoodCategory.Protein, "catfish"), ("Bò xào trứng", FoodCategory.Protein, "beef"),
    ("Thịt bò nạc nấu chín", FoodCategory.Protein, "beef"), ("Bơ", FoodCategory.Fruits, "avocado"), ("Bơ", FoodCategory.Fats, "butter"), ("Bơ đậu phộng", FoodCategory.Fats, "peanuts"),
    ("bo xao trung", null, "eggs"), ("Trung ga luoc", null, "eggs"), ("Ngô luộc", null, "corn"), ("Ngò rí", null, "cilantro"), ("Hành nhỏ", null, "onion"),
    ("Beef, ground, 80% lean meat / 20% fat, raw", null, "ground_beef"), ("Oil, olive, salad or cooking", null, "olive_oil"), ("Egg, white, raw", null, "egg_white"),
    ("Eggs, Grade A, Large, egg whole", null, "eggs"), ("Eggplant, raw", null, "eggplant"), ("Rice noodles", null, "rice_noodles"), ("Cheese, cheddar", null, "cheddar"),
    ("Water spinach", null, "spinach"), ("Peanut butter, smooth", null, "peanuts"), ("Hamburger", null, "ground_beef"), ("Phở bò", FoodCategory.FastFood, "rice_noodles"),
    ("Bún bò Huế", FoodCategory.FastFood, "rice_noodles"), ("Nước mắm", null, "fish_sauce"), ("Nước tương", null, "soy_sauce"), ("Sữa chua không đường", FoodCategory.Dairy, "yogurt"),
    ("Cơm gạo lứt", FoodCategory.Grains, "brown_rice"), ("Khoai tây chiên", FoodCategory.FastFood, "potato"), ("Cà phê sữa đá", FoodCategory.Beverages, "coffee"),
    ("Bánh mì sandwich", FoodCategory.Grains, null), ("Bánh mì thịt", FoodCategory.FastFood, null), ("Đậu hũ", FoodCategory.Protein, null), ("Yến mạch khô", FoodCategory.Grains, null),
    ("Nước ngọt có gas", FoodCategory.Beverages, null), ("Xyz không rõ", FoodCategory.Other, null), ("", FoodCategory.Fruits, null),
    ("Mồng tơi", FoodCategory.Vegetables, "cabbage"), ("Quả lạ chưa biết", FoodCategory.Fruits, "apple"), ("Sữa gì đó", FoodCategory.Dairy, "milk"),
];
var iconWrong = iconCases.Where(c => IconKeyResolver.Resolve(c.Name, c.Cat) != c.Expect).Select(c => $"{c.Name}→{IconKeyResolver.Resolve(c.Name, c.Cat) ?? "null"} (cần {c.Expect ?? "null"})").ToList();
Check(iconWrong.Count == 0, $"Icon: resolver đúng {iconCases.Length} ca EN/VN có dấu/không dấu, cụm dài, bò/bơ, ngô/ngò, null; sai: {string.Join("; ", iconWrong)}");
Check(IconKeyResolver.Resolve("Cherry tomatoes") == "cherry_tomato" && IconKeyResolver.Resolve("Soy sauce") == "soy_sauce" && IconKeyResolver.Resolve("Almond flour") == "almond_flour", "Icon: cụm dài thắng cụm ngắn (cherry tomato › tomato, soy sauce, almond flour)");
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

Console.WriteLine(Environment.ExitCode == 0 ? "ALL PASS (incl. ALG-02, ALG-03, ALG-04, MOS-005, Dashboard S9, Expenditure flux, AI Plate)" : "SOME FAIL");

sealed class StubAnalyzer : IAiMealAnalyzer
{
    public Task<AiMealDraft> AnalyzeAsync(IReadOnlyList<MealPhoto> photos, string? description, CancellationToken ct)
        => Task.FromResult(new AiMealDraft([new AiDraftDish("Egg dish", [new AiDraftIngredient("Egg", 50)])], "low"));

    public Task<AiIngredientEstimate> EstimateIngredientAsync(string name, decimal grams, CancellationToken ct)
        => Task.FromResult(new AiIngredientEstimate(70, 6, 5, 0.5m));
}

// Giả rỗng cho check không cần DB: ghi nhận Add, SaveChanges = 1 dòng
class FakeUow : IUnitOfWork
{
    public readonly List<object> Added = [];
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
