using Microsoft.EntityFrameworkCore;
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
Check(ExpenditureCalculator.Status(ok7) == ExpenditureStatus.Updating, "ST-01 Updating");
Check(ExpenditureCalculator.Status(ok7) == ExpenditureStatus.Updating && ExpenditureCalculator.IsCalibrating(w0.AddDays(10), w0, 14) && !ExpenditureCalculator.IsCalibrating(w0.AddDays(14), w0, 14),
    "#3: trong 14 ngày đầu vẫn Updating khi đủ dữ liệu (không đóng băng); Calibrating chỉ là nhãn (< 14 ngày kể từ start)");
var noWeigh = ok7.Select(d => d with { TrendKg = null }).ToList();
Check(ExpenditureCalculator.Status(noWeigh) == ExpenditureStatus.Holding, "ST-01 Holding không cân");
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
// ETA neo vào lần cân thật cuối (vector tính tay): −1 kg/tuần = −1/7 kg/ngày, còn 8 kg ⇒ 56 ngày sau lần cân cuối; không dùng hôm nay làm mốc, qua cổng TRA + G = 14 + stale
string? EtaGate(IReadOnlyList<(DateOnly Date, decimal Kg)> pts, DateOnly today) => WeightTrendSeries.RateGate(pts.Select(p => p.Date), today).Reason;
GoalProgressCalculator.EtaResult EtaAt(IReadOnlyList<(DateOnly Date, decimal Kg)> pts, decimal? target, GoalDirection dir, DateOnly today) => GoalProgressCalculator.Eta(pts, target, dir, today, EtaGate(pts, today));
var gEta = EtaAt(gPoints, 80m, GoalDirection.Loss, g0.AddDays(14));
Check(gEta is { Status: "Estimated", ReasonCode: null } && gEta.Date == g0.AddDays(14 + 56), "Goal ETA: −1 kg/tuần, còn 8 kg → 56 ngày sau lần cân cuối (hôm nay = ngày cân cuối)");
var gWrong = Enumerable.Range(0, 15).Select(i => (g0.AddDays(i), 88m + 0.1m * i)).ToList();   // goal giảm cân (80) nhưng cân đang tăng
Check(EtaAt(gWrong, 80m, GoalDirection.Loss, g0.AddDays(14)) is { Date: null, Status: "Unavailable", ReasonCode: "RATE_NOT_TOWARD_GOAL" }, "Goal ETA null + RATE_NOT_TOWARD_GOAL khi đi sai hướng");
{
    // Dữ liệu không đổi, hôm nay trôi 3 và 5 ngày: ETA KHÔNG trôi về sau (trước đây today + 56 ⇒ d73, d75; giờ đứng yên ở d70)
    var etaD17 = EtaAt(gPoints, 80m, GoalDirection.Loss, g0.AddDays(17)); var etaD19 = EtaAt(gPoints, 80m, GoalDirection.Loss, g0.AddDays(19));
    Check(etaD17.Date == g0.AddDays(70) && etaD19.Date == g0.AddDays(70) && g0.AddDays(17 + 56) != etaD17.Date && g0.AddDays(19 + 56) != etaD19.Date, "Goal ETA: không có lần cân mới, hôm nay trôi ⇒ ETA đứng yên (d70), không trôi về sau (công thức cũ d73/d75)");

    // Chuỗi hằng ngày d0..d20, 90 − 0.1 mỗi ngày (88.0 ở d20), mục tiêu 80: tốc độ −0.1/ngày ⇒ còn 8 kg = 80 ngày ⇒ d100
    var etaDaily = Enumerable.Range(0, 21).Select(i => (g0.AddDays(i), 90m - 0.1m * i)).ToList();
    var etaD25 = EtaAt(etaDaily, 80m, GoalDirection.Loss, g0.AddDays(25)); var etaD26 = EtaAt(etaDaily, 80m, GoalDirection.Loss, g0.AddDays(26));
    Check(etaD25.Date == g0.AddDays(100) && etaD26.Date == g0.AddDays(100), "Goal ETA: không cân 5–6 ngày vẫn d100 (neo lần cân cuối d20 + 80)");
    // Ranh giới ngày local (múi giờ user): lần cân cuối d20 còn trong 7 ngày ở d26, sang d27 là stale ⇒ ETA null + STALE_WEIGHT_DATA, không đoán
    Check(EtaAt(etaDaily, 80m, GoalDirection.Loss, g0.AddDays(27)) is { Date: null, Status: "Unavailable", ReasonCode: "STALE_WEIGHT_DATA" } && EtaAt(etaDaily, 80m, GoalDirection.Loss, g0.AddDays(30)).ReasonCode == "STALE_WEIGHT_DATA",
        "Goal ETA: 7+ ngày không cân (stale) ⇒ null + STALE_WEIGHT_DATA; ranh giới theo ngày local của user");

    // Cổng TRA: chỉ 2 lần cân ⇒ INSUFFICIENT_WEIGHT_DATA
    var etaTwo = new List<(DateOnly, decimal)> { (g0, 90m), (g0.AddDays(14), 88m) };
    Check(EtaAt(etaTwo, 80m, GoalDirection.Loss, g0.AddDays(14)) is { Date: null, ReasonCode: "INSUFFICIENT_WEIGHT_DATA" }, "Goal ETA: TRA gate fail (2 lần cân) ⇒ null + INSUFFICIENT_WEIGHT_DATA");

    // Guard G = 14: d0..d5 rồi d27..d34 (khoảng trống 22 ngày cắt ngang biên cửa sổ) ⇒ INSUFFICIENT; khoảng đúng 14 ngày (d5 → d19) vẫn qua và có ETA
    var etaGap = Enumerable.Range(0, 6).Concat(Enumerable.Range(27, 8)).Select((d, k) => (g0.AddDays(d), 90m - 0.1m * k)).ToList();
    var etaGap14 = Enumerable.Range(0, 6).Concat(Enumerable.Range(19, 8)).Select((d, k) => (g0.AddDays(d), 90m - 0.1m * k)).ToList();
    Check(EtaAt(etaGap, 80m, GoalDirection.Loss, g0.AddDays(34)) is { Date: null, ReasonCode: "INSUFFICIENT_WEIGHT_DATA" } && EtaGate(etaGap, g0.AddDays(34)) == "INSUFFICIENT_WEIGHT_DATA"
          && EtaAt(etaGap14, 80m, GoalDirection.Loss, g0.AddDays(26)) is { Status: "Estimated" } r14 && r14.Date == g0.AddDays(26 + 87),
        "Goal ETA: guard G = 14 (gap 22 ngày ⇒ null + INSUFFICIENT_WEIGHT_DATA; gap đúng 14 ngày ⇒ ETA d113 = d26 + 87)");

    // Weight gain, maintenance, không có target
    var etaGain = Enumerable.Range(0, 15).Select(i => (g0.AddDays(i), 90m + 0.1m * i)).ToList();
    Check(EtaAt(etaGain, 95m, GoalDirection.Gain, g0.AddDays(14)).Date == g0.AddDays(14 + 36) && EtaAt(etaGain, 80m, GoalDirection.Loss, g0.AddDays(14)).ReasonCode == "RATE_NOT_TOWARD_GOAL"
          && EtaAt(etaGain, 80m, GoalDirection.Maintain, g0.AddDays(14)) is { Date: null, Status: "NotApplicable", ReasonCode: null } && EtaAt(etaGain, null, GoalDirection.Loss, g0.AddDays(14)).ReasonCode == "NO_TARGET_WEIGHT",
        "Goal ETA: Gain đúng d50 (còn 3.6 kg ÷ 0.1 = 36 ngày), Loss mà đang tăng ⇒ RATE_NOT_TOWARD_GOAL, Maintenance NotApplicable, thiếu target NO_TARGET_WEIGHT");

    // Tốc độ 0 (cân phẳng) ⇒ RATE_NOT_TOWARD_GOAL, không ETA vô hạn
    var etaFlat = Enumerable.Range(0, 15).Select(i => (g0.AddDays(i), 88m)).ToList();
    Check(EtaAt(etaFlat, 80m, GoalDirection.Loss, g0.AddDays(14)) is { Date: null, ReasonCode: "RATE_NOT_TOWARD_GOAL" }, "Goal ETA: tốc độ 0 ⇒ null + RATE_NOT_TOWARD_GOAL");

    // Đã đạt Goal Weight (trend lần cân cuối chạm/vượt, kể cả bằng đúng) ⇒ GoalAchieved, không ETA giả, kể cả khi dữ liệu stale
    Check(EtaAt(etaDaily, 88m, GoalDirection.Loss, g0.AddDays(20)) is { Date: null, Status: "GoalAchieved", ReasonCode: null } && EtaAt(etaDaily, 90m, GoalDirection.Loss, g0.AddDays(40)) is { Status: "GoalAchieved" }
          && EtaAt(etaGain, 91m, GoalDirection.Gain, g0.AddDays(14)) is { Date: null, Status: "GoalAchieved" }, "Goal ETA: đã đạt target (loss ≤ / gain ≥) ⇒ GoalAchieved, không có ngày ETA giả");

    // Không bao giờ trả ngày quá khứ như dự báo tương lai: lần cân cuối d20 = 80.0, mục tiêu 79.5, −1 kg/ngày ⇒ dự báo d21; hôm nay d26 ⇒ null + ETA_IN_PAST; hôm nay d21 ⇒ d21 (chưa quá khứ)
    var etaFast = Enumerable.Range(0, 21).Select(i => (g0.AddDays(i), 100m - i)).ToList();
    Check(EtaAt(etaFast, 79.5m, GoalDirection.Loss, g0.AddDays(26)) is { Date: null, Status: "Unavailable", ReasonCode: "ETA_IN_PAST" } && EtaAt(etaFast, 79.5m, GoalDirection.Loss, g0.AddDays(21)).Date == g0.AddDays(21),
        "Goal ETA: dự báo từ lần cân cuối đã rơi vào quá khứ mà chưa đạt ⇒ null + ETA_IN_PAST (không trả ngày quá khứ)");
}
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
    && CheckInCalculator.ReasonCodes(prPlain with { Clamped = true, FloorApplied = true }, ExpenditureStatus.Updating, 0.8m, 0.5m, 0).SequenceEqual(["CLAMPED", "FLOOR_APPLIED"])
    && CheckInCalculator.ReasonCodes(prPlain, ExpenditureStatus.Updating, 0.2m, 0.5m, 0).Count == 0
    && CheckInCalculator.ReasonCodes(prPlain, ExpenditureStatus.Updating, 0.5m, 0.5m, 0).Count == 0
    && CheckInCalculator.ReasonCodes(prPlain, ExpenditureStatus.Updating, 0.8m, 0.5m, 0, GoalDirection.Loss, emitRateVerdict: true).SequenceEqual(["FASTER_THAN_TARGET"]), "Check-in A1: mặc định KHÔNG phát FASTER/SLOWER/ON_TARGET (trung lập, chỉ CALIBRATING/HOLDING/INCOMPLETE_DAYS/CLAMPED/FLOOR); bộ phân loại giữ nguyên sau cờ emitRateVerdict");
// A1: không bao giờ biến thiếu/không chắc chắn thành ON_TARGET, kể cả rate bằng đúng mục tiêu; Reason trung lập không kết luận nhanh/chậm/đúng mục tiêu; không đổi kcal đề xuất
{
    var neutralAll = new[] { GoalDirection.Loss, GoalDirection.Gain, GoalDirection.Maintain }.SelectMany(d => new decimal?[] { null, 0m, 0.5m, -0.5m }
        .Select(r => (CheckInCalculator.ReasonCodes(prPlain, ExpenditureStatus.Updating, r, 0.5m, 0, d), CheckInCalculator.Reason(prPlain, ExpenditureStatus.Updating, r, 0.5m, d)))).ToList();
    var verdictCodes = new[] { "FASTER_THAN_TARGET", "SLOWER_THAN_TARGET", "ON_TARGET", "LOSING_WEIGHT", "GAINING_WEIGHT" };
    Check(neutralAll.All(x => !x.Item1.Intersect(verdictCodes).Any()) && neutralAll.All(x => !x.Item2.Contains("nhanh hơn") && !x.Item2.Contains("chậm hơn") && !x.Item2.Contains("Đúng tốc độ") && !x.Item2.Contains("Đúng mục tiêu")),
        "Check-in A1: mọi hướng goal/rate (null, 0, = mục tiêu, âm, dương) không phát mã phán quyết và không có chữ nhanh/chậm/đúng mục tiêu");
    Check(CheckInCalculator.Reason(prPlain, ExpenditureStatus.Updating, 0.5m, 0.5m, GoalDirection.Loss).Contains("chưa đủ cơ sở")
        && CheckInCalculator.Reason(prPlain, ExpenditureStatus.Updating, null, 0.5m, GoalDirection.Loss) == "Giữ nguyên target", "Check-in A1: có rate ⇒ thông tin tiến độ trung lập; rate null ⇒ không bịa thông tin");
}
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

// ===== C7: dấu tốc độ + hướng theo GoalType, MỘT nguồn GoalRules (MuscleGain từng bị lưu như giảm cân) =====
var c7Opt = new AlgorithmOptions();
var c7Today = new DateOnly(2026, 10, 19);
var c7Profile = new UserProfile { UserId = Guid.NewGuid(), Gender = Gender.Male, DateOfBirth = new DateOnly(1996, 1, 1), HeightCm = 175m, ActivityLevel = ActivityLevel.ModeratelyActive, TrainingType = TrainingType.None };
Check(Enum.GetValues<GoalType>().All(t => { GoalRules.DirectionOf(t); GoalRules.SignedRate(t, 0.5m); return true; }), "C7: mọi GoalType đều được khai báo tường minh trong GoalRules (không throw)");
try { GoalRules.SignedRate((GoalType)99, 0.5m); Check(false, "C7: SignedRate enum lạ phải throw"); }
catch (ArgumentOutOfRangeException) { Check(true, "C7: SignedRate enum lạ throw (không âm thầm coi là giảm cân)"); }
try { GoalRules.DirectionOf((GoalType)99); Check(false, "C7: DirectionOf enum lạ phải throw"); }
catch (ArgumentOutOfRangeException) { Check(true, "C7: DirectionOf enum lạ throw"); }
Check(GoalRules.SignedRate(GoalType.WeightLoss, 0.5m) == 0.5m && GoalRules.SignedRate(GoalType.WeightLoss, -0.5m) == 0.5m
    && GoalRules.SignedRate(GoalType.WeightGain, 0.5m) == -0.5m && GoalRules.SignedRate(GoalType.WeightGain, -0.5m) == -0.5m
    && GoalRules.SignedRate(GoalType.MuscleGain, 0.3m) == -0.3m && GoalRules.SignedRate(GoalType.MuscleGain, -0.3m) == -0.3m
    && GoalRules.SignedRate(GoalType.Maintenance, 0.7m) == 0m, "C7: SignedRate — Loss +|r|, WeightGain/MuscleGain −|r| (bất kể dấu đầu vào), Maintenance 0");
Check(GoalRules.SignedRate(GoalType.Endurance, 0.5m) == 0.5m && GoalRules.SignedRate(GoalType.Flexibility, -0.5m) == 0.5m
    && GoalRules.DirectionOf(GoalType.Endurance) == GoalDirection.Maintain && GoalRules.DirectionOf(GoalType.Flexibility) == GoalDirection.Maintain,
    "C7: Endurance/Flexibility giữ hành vi cũ (SignedRate +|r| như giảm, DirectionOf Maintain) — CHỜ user quyết định");
Check(GoalRules.DirectionOf(GoalType.WeightLoss) == GoalDirection.Loss && GoalRules.DirectionOf(GoalType.WeightGain) == GoalDirection.Gain
    && GoalRules.DirectionOf(GoalType.MuscleGain) == GoalDirection.Gain && GoalRules.DirectionOf(GoalType.Maintenance) == GoalDirection.Maintain, "C7: DirectionOf — Loss, WeightGain/MuscleGain = Gain, Maintenance = Maintain");

// BuildProgram (Create) + ProgramWeekBuilder: dấu rate, kg/tuần, kcal so với TDEE 2700 (80 kg, 0.5 %/tuần = 0.4 kg = 440 kcal/ngày)
MacroProgram C7Build(GoalType t, decimal rate = 0.5m)
    => ProgramDraftCalculator.BuildProgram(new CreateProgramCommand(t, rate, ProgramType.Coached, DietStyle.Balanced, ProteinLevel.Moderate, null, null, 2700, TargetWeightKg: 85m), c7Profile, 80m, c7Today);
int C7Kcal(MacroProgram p) => ProgramWeekBuilder.Compute(p, 80m, 2700, c7Opt).Calories;
var c7Loss = C7Build(GoalType.WeightLoss); var c7WGain = C7Build(GoalType.WeightGain); var c7MGain = C7Build(GoalType.MuscleGain);
var c7Maint = C7Build(GoalType.Maintenance, 0m); var c7End = C7Build(GoalType.Endurance); var c7Flex = C7Build(GoalType.Flexibility);
Check(c7Loss.WeeklyWeightChangeTargetPercent == 0.5m && c7Loss.WeeklyWeightChangeTargetKg == -0.4m && C7Kcal(c7Loss) == 2260, "C7 Create: WeightLoss rate +0.5, −0.4 kg/tuần, thâm hụt 440 → 2260 kcal");
Check(c7WGain.WeeklyWeightChangeTargetPercent == -0.5m && c7WGain.WeeklyWeightChangeTargetKg == 0.4m && C7Kcal(c7WGain) == 3140, "C7 Create: WeightGain rate −0.5, +0.4 kg/tuần, dư 440 → 3140 kcal");
Check(c7MGain.GoalType == GoalType.MuscleGain && c7MGain.WeeklyWeightChangeTargetPercent == -0.5m && c7MGain.WeeklyWeightChangeTargetKg == 0.4m && C7Kcal(c7MGain) == 3140 && C7Kcal(c7MGain) > 2700,
    "C7 Create: MuscleGain giữ GoalType, rate âm (tăng), kcal > TDEE (không còn bị lưu như giảm cân)");
Check(c7Maint.WeeklyWeightChangeTargetPercent == 0m && c7Maint.WeeklyWeightChangeTargetKg == 0m && C7Kcal(c7Maint) == 2700, "C7 Create: Maintenance rate 0 → kcal = TDEE");
Check(c7End.WeeklyWeightChangeTargetPercent == 0.5m && C7Kcal(c7End) == 2260 && c7Flex.WeeklyWeightChangeTargetPercent == 0.5m && C7Kcal(c7Flex) == 2260,
    "C7 Create: Endurance/Flexibility giữ hành vi cũ (rate dương như giảm cân) — CHỜ user quyết định");
Check(C7Build(GoalType.MuscleGain, -0.5m).WeeklyWeightChangeTargetPercent == -0.5m && C7Build(GoalType.WeightLoss, -0.5m).WeeklyWeightChangeTargetPercent == 0.5m, "C7 Create: dấu client gửi lên bị bỏ qua, chỉ độ lớn được dùng");

// Update goal: gọi đúng ProgramGoalUpdate.Resign (handler dùng cùng hàm) trên program dựng bằng BuildProgram; đổi loại giữ độ lớn cũ, nhập rate mới thì ghi đè
MacroProgram C7Update(GoalType from, decimal storedRate, GoalType to, decimal? requested)
{
    var p = C7Build(from, storedRate);
    p.GoalType = to;
    ProgramGoalUpdate.Resign(p, requested, 80m);
    return p;
}
var c7U1 = C7Update(GoalType.WeightLoss, 0.5m, GoalType.MuscleGain, null);
var c7U2 = C7Update(GoalType.MuscleGain, 0.3m, GoalType.WeightLoss, null);
Check(c7U1.WeeklyWeightChangeTargetPercent == -0.5m && c7U1.WeeklyWeightChangeTargetKg == 0.4m && c7U2.WeeklyWeightChangeTargetPercent == 0.3m && c7U2.WeeklyWeightChangeTargetKg == -0.24m
    && C7Update(GoalType.WeightLoss, 0.5m, GoalType.MuscleGain, 0.2m).WeeklyWeightChangeTargetPercent == -0.2m && C7Update(GoalType.MuscleGain, 0.3m, GoalType.Maintenance, 0.4m) is { WeeklyWeightChangeTargetPercent: 0m, WeeklyWeightChangeTargetKg: 0m }
    && C7Update(GoalType.WeightLoss, 0.5m, GoalType.MuscleGain, 0.25m).WeeklyWeightChangeTargetKg == 0.2m,
    "C7 Update (ProgramGoalUpdate.Resign): đổi Loss→MuscleGain được rate âm và +kg/tuần, MuscleGain→Loss ngược lại, rate mới ghi đè, Maintenance = 0");

// Preview == Create cho cùng đầu vào (TDEE nhập tay 2700, không sàn, không shift, đích 85 kg)
ProgramPreviewResponse C7Preview(GoalType t, decimal rate = 0.5m)
    => ProgramPreviewCalculator.Compute(c7Profile, 80m, new ProgramPreviewInput(t, 85m, rate, DietStyle.Balanced, ProteinLevel.Moderate, CalorieFloorChoice.Off, TrainingType.None, CalorieShiftMode.Even, null, null, 2700), 7700, c7Today);
bool C7Agree(GoalType t, decimal rate = 0.5m)
{
    var pv = C7Preview(t, rate); var pg = C7Build(t, rate);
    return pv.AverageCalories == C7Kcal(pg) && pv.WeeklyChangeKg == pg.WeeklyWeightChangeTargetKg && pv.WeeklyChangePercent == -pg.WeeklyWeightChangeTargetPercent;
}
Check(Enum.GetValues<GoalType>().All(t => C7Agree(t, t == GoalType.Maintenance ? 0m : 0.5m)), "C7: Preview và Create cùng calories, kg/tuần, %/tuần cho mọi GoalType (kể cả MuscleGain, Endurance, Flexibility)");
var c7PvM = C7Preview(GoalType.MuscleGain);
Check(c7PvM.AverageCalories == 3140 && c7PvM.WeeklyChangeKg == 0.4m && c7PvM.ProjectedEndDate == c7Today.AddDays((int)Math.Ceiling(5m / 0.4m * 7m)) && C7Preview(GoalType.WeightGain).ProjectedEndDate == c7PvM.ProjectedEndDate,
    "C7 Preview (regression guard): MuscleGain 3140 kcal, +0.4 kg/tuần, đích 85 kg có ngày dự kiến giống WeightGain");
Check(C7Preview(GoalType.WeightLoss).ProjectedEndDate is null, "C7 Preview: đích cao hơn cân hiện tại với program giảm → không có ngày dự kiến");

// Check-in (ALG-04): program MuscleGain −0.3 %/tuần ở trend 80 → mục tiêu +0.24 kg/tuần (rateTargetKg −0.24, dương = giảm); TDEE ước lượng lại quyết định kcal
var c7Prog = C7Build(GoalType.MuscleGain, 0.3m);   // program do BuildProgram dựng: bản cũ lưu +0.3 (giảm) nên các check dưới đây FAIL trên bản cũ
var c7Prev = new MacroProgramWeek { CaloriesTarget = 3064, ProteinTarget = 160m };
var c7Slow = CheckInCalculator.Propose(c7Prog, 80m, 2900m, c7Prev, c7Opt);   // tăng chậm hơn mục tiêu ⇒ TDEE ước lượng cao hơn ⇒ ăn thêm
var c7Fast = CheckInCalculator.Propose(c7Prog, 80m, 2700m, c7Prev, c7Opt);   // tăng nhanh hơn ⇒ TDEE thấp hơn ⇒ bớt
Check(c7Slow.RateTargetKg == -0.24m && c7Slow.RawCalories == 3164 && c7Slow.NewCalories == 3164 && c7Slow.Delta == 100 && !c7Slow.Clamped, $"C7 Check-in: MuscleGain tăng chậm hơn mục tiêu → kcal TĂNG ({c7Slow.PreviousCalories} → {c7Slow.NewCalories})");
Check(c7Fast.RawCalories == 2964 && c7Fast.NewCalories == 2964 && c7Fast.Delta == -100 && !c7Fast.Clamped, $"C7 Check-in: MuscleGain tăng nhanh hơn mục tiêu → kcal GIẢM ({c7Fast.PreviousCalories} → {c7Fast.NewCalories})");
var c7WGainProg = C7Build(GoalType.WeightGain, 0.3m);
Check(CheckInCalculator.Propose(c7WGainProg, 80m, 2900m, c7Prev, c7Opt).NewCalories == c7Slow.NewCalories, "C7 Check-in: program MuscleGain và WeightGain (đều từ BuildProgram) cùng rate cho cùng đề xuất kcal");

// Edit Goal: MuscleGain dùng dải Gain, giữ loại khi cùng hướng, đổi hướng thì buộc program mới; Endurance/Flexibility giữ hành vi cũ (Maintain)
var c7Mg = GoalEditCalculator.Compute(EgState(GoalType.MuscleGain, 85m, rate: -0.3m), 86m, 0.3m, optW, egToday);
Check(c7Mg.Preview is { RequiresNewProgram: false, GoalType: "MuscleGain", WeeklyRatePercent: 0.3m } && c7Mg.ProgramRatePercent == -0.3m && c7Mg.NewGoalType == GoalType.MuscleGain && c7Mg.SignedWeeklyKg == 0.24m,
    "C7 Edit Goal: MuscleGain cùng hướng tăng → giữ loại, rate chương trình −0.3 (tăng), +0.24 kg/tuần");
Check(GoalEditCalculator.Compute(EgState(GoalType.MuscleGain, 85m, rate: -0.3m), 75m, 0.5m, optW, egToday).Preview is { RequiresNewProgram: true, GoalType: "WeightLoss", CurrentGoalType: "MuscleGain" }, "C7 Edit Goal: MuscleGain đổi đích xuống thấp hơn → đổi hướng Loss, buộc tạo program mới");
try { GoalEditCalculator.Compute(EgState(GoalType.MuscleGain, 85m, rate: -0.3m), 86m, 0.6m, optW, egToday); Check(false, "C7 Edit Goal: MuscleGain 0.6 %/tuần phải bị từ chối"); }
catch (BadRequestException) { Check(true, "C7 Edit Goal (regression guard): MuscleGain bị giới hạn bởi dải Gain (0.5 %), không phải dải Loss (1 %)"); }
Check(GoalEditCalculator.Compute(EgState(GoalType.Endurance, 80m, rate: 0m), 80.05m, null, optW, egToday).Preview is { RequiresNewProgram: false, GoalType: "Endurance" }
    && GoalEditCalculator.Compute(EgState(GoalType.Flexibility, 80m, rate: 0m), 85m, 0.2m, optW, egToday).Preview is { RequiresNewProgram: true, GoalType: "WeightGain" },
    "C7 Edit Goal: Endurance/Flexibility giữ hành vi cũ (coi như Maintain: đích ≈ cân giữ loại, đích tăng → WeightGain + program mới) — CHỜ user quyết định");

// ===== C7 Calorie Shift: chặn tổ hợp cho ngày ≤ 0 kcal / dưới sàn / carbs < 0 (viết qua API hiện có để chạy được cả trước khi sửa) =====
// Maintenance, TDEE nhập tay 2700, 80 kg, Moderate protein 1.8 g/kg, Balanced ⇒ tuần: 2700 kcal, P 144, F 90, C 328.5
ProgramPreviewResponse CsPreview(CalorieShiftMode mode, decimal? pct, DayOfWeek[] days, CalorieFloorChoice floor = CalorieFloorChoice.Off)
    => ProgramPreviewCalculator.Compute(c7Profile, 80m, new ProgramPreviewInput(GoalType.Maintenance, null, 0m, DietStyle.Balanced, ProteinLevel.Moderate, floor, TrainingType.None, mode, days, pct, 2700), 7700, c7Today);
bool CsRejected(Action a) { try { a(); return false; } catch (BadRequestException) { return true; } }
// Xem trước KHÔNG ném khi shift không khả thi: trả FeasibilityResponse (Create/Review-draft vẫn ném 400)
bool CsInfeasible(Func<ProgramPreviewResponse> f) => f().Feasibility is { IsFeasible: false };
DayOfWeek[] CsDays(int n) => new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }.Take(n).ToArray();

Check(CsInfeasible(() => CsPreview(CalorieShiftMode.Custom, 30m, CsDays(6))), "Calorie Shift: 30% × 6 ngày (f·s/(7−s) = 1.8 ≥ 1 ⇒ ngày còn lại −0.8 × tuần) bị chặn, không ra kcal âm");
Check(CsInfeasible(() => CsPreview(CalorieShiftMode.Custom, 100m / 6m * 1.0001m, CsDays(6))) && !CsInfeasible(() => CsPreview(CalorieShiftMode.Custom, 8m, CsDays(6))),
    "Calorie Shift: vừa quá biên toán học 6 ngày (16.67%, ngày còn lại ≤ 0) bị chặn; 8% × 6 ngày (1404 kcal, carbs +4.5 g) vẫn qua");
Check(CsInfeasible(() => CsPreview(CalorieShiftMode.Custom, 30m, CsDays(5), CalorieFloorChoice.Standard)), "Calorie Shift: ngày còn lại (675 kcal) thấp hơn sàn kcal 1749 bị chặn");
Check(CsInfeasible(() => CsPreview(CalorieShiftMode.Custom, 20m, CsDays(5))), "Calorie Shift: carbs ngày thấp xuống dưới 0 (1350 kcal ⇒ carbs −9 g) bị chặn thay vì kẹp 0 làm macro lệch kcal");
var csOk = CsPreview(CalorieShiftMode.Custom, 15m, CsDays(3));
Check(csOk.Days.Single(d => d.DayOfWeek == "Monday").Calories == 3105 && csOk.Days.Single(d => d.DayOfWeek == "Sunday").Calories == 2396 && csOk.Days.Single(d => d.DayOfWeek == "Sunday").CarbsG == 328.5m + (2396 - 2700) / 4m,
    "Calorie Shift: tổ hợp hợp lệ (15% × 3 ngày) giữ nguyên số: ngày chọn 3105, ngày còn lại 2396, carbs = carbs tuần + Δkcal/4");
Check(Math.Abs(csOk.Days.Sum(d => d.Calories) - 7 * 2700) <= 4 && CsPreview(CalorieShiftMode.Custom, 30m, CsDays(1)).Days.Sum(d => d.Calories) is var csSum1 && Math.Abs(csSum1 - 7 * 2700) <= 4, "Calorie Shift: tổng tuần giữ nguyên (sai số làm tròn ≤ 4 kcal) cho tổ hợp hợp lệ");
Check(Enum.GetValues<CalorieShiftMode>().Where(m => m == CalorieShiftMode.Even).All(m => CsPreview(m, 30m, CsDays(6)).Days.All(d => d.Calories == 2700)), "Calorie Shift: mode Even không dịch kcal dù có gửi percent/days");
var csDraft = new CreateProgramCommand(GoalType.Maintenance, 0m, ProgramType.Coached, DietStyle.Balanced, ProteinLevel.Moderate, null, null, 2700, null, null, CalorieShiftMode.Custom, CsDays(6).ToList(), 30m);
var csDraftProgram = ProgramDraftCalculator.BuildProgram(csDraft, c7Profile, 80m, c7Today);
Check(CsRejected(() => ProgramDraftCalculator.Days(csDraft, csDraftProgram, 80m, 2700, c7Opt)), "Calorie Shift: Review (PreviewNewProgram/Days) cũng chặn tổ hợp không khả thi, cùng quy tắc với onboarding preview");

// --- API dùng chung CalorieShiftFeasibility (Create + Review + Preview cùng gọi) ---
var csWeek = new MacroTargets(2700, 144m, 328.5m, 90m);
var csBigCarbWeek = new MacroTargets(2700, 144m, 900m, 90m);   // carbs thừa để cô lập lỗi kcal/sàn khỏi lỗi carbs
DaysOfWeek CsFlags(int n) => CalorieShiftCalculator.ToFlags(CsDays(n));
List<CalorieShiftViolationModel> CsViol(MacroTargets w, decimal pct, int n, int? floor = null) => CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, pct, CsFlags(n), floor);

Check(CsViol(csBigCarbWeek, 40m, 5).Any(v => v.Code == CalorieShiftFeasibility.NonPositiveCalories && v.Calories == 0), "Calorie Shift: hệ số ngày còn lại đúng 0 (40% × 5 ngày, f·s/(7−s) = 1) ⇒ 0 kcal bị coi là không hợp lệ (≤ 0)");
Check(CsViol(csBigCarbWeek, 17m, 6).Any(v => v.Code == CalorieShiftFeasibility.NonPositiveCalories && v.Day == "Sunday" && v.Calories < 0) && CsViol(csBigCarbWeek, 16m, 6).Count == 0,
    "Calorie Shift: biên toán học 6 ngày — 17% ra kcal âm (bị chặn), 16% còn dương (qua) khi carbs không ràng buộc");
var csFloor = CsViol(csBigCarbWeek, 30m, 5, 1800);
Check(csFloor.Count == 2 && csFloor.All(v => v.Code == CalorieShiftFeasibility.BelowFloor && v.Calories == 675) && csFloor.Select(v => v.Day).SequenceEqual(["Saturday", "Sunday"]) && CsViol(csBigCarbWeek, 30m, 5, 675).Count == 0 && CsViol(csBigCarbWeek, 30m, 5, 676).Count == 2,
    "Calorie Shift: dưới sàn — 2 ngày còn lại (Saturday, Sunday) 675 kcal vi phạm sàn 1800 và 676; đúng bằng sàn (675) thì hợp lệ (≥ sàn)");
var csCarb = CsViol(csWeek, 20m, 5);
Check(csCarb.Count == 2 && csCarb.All(v => v.Code == CalorieShiftFeasibility.CarbsBelowZero && v.Calories == 1350 && v.CarbsG == 328.5m + (1350 - 2700) / 4m) && csCarb.Select(v => v.Day).SequenceEqual(["Saturday", "Sunday"]),
    "Calorie Shift: carbs âm — 20% × 5 ngày: ngày còn lại 1350 kcal (dương, không sàn) nhưng carbs −9 g ⇒ vi phạm CARBS_BELOW_ZERO với carbs chưa kẹp");
Check(CalorieShiftFeasibility.Validate(csWeek, CalorieShiftMode.Even, 30m, CsFlags(6), 3000).Count == 0 && CalorieShiftFeasibility.Validate(csWeek, CalorieShiftMode.Custom, null, CsFlags(6), 3000).Count == 0
    && CalorieShiftFeasibility.Validate(csWeek, CalorieShiftMode.Custom, 30m, DaysOfWeek.None, 3000).Count == 0, "Calorie Shift: không có shift thật (Even / không % / không chọn ngày) thì không vi phạm dù có sàn");

// Lưới tổ hợp: tổ hợp hợp lệ giữ nguyên số của ForDay cũ + tổng tuần; tổ hợp không hợp lệ ⇒ mọi option trả về đều hợp lệ và tối đại
var csGridOk = 0; var csGridBad = 0; var csGridOptionsOk = true; var csGridValidSame = true; var csGridSum = true; var csGridPercentOffered = true; var csGridDaysOffered = true;
foreach (var w in new[] { csWeek, csBigCarbWeek, new MacroTargets(1800, 120m, 150m, 60m) })
foreach (var floor in new int?[] { null, 1500, 2000 })
foreach (var pct in new[] { 5m, 10m, 12.5m, 15m, 20m, 25m, 30m })
foreach (var n in Enumerable.Range(1, 6))
{
    var days = CsFlags(n);
    var viol = CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, pct, days, floor);
    if (viol.Count == 0)
    {
        csGridOk++;
        var sum = 0;
        foreach (var d in ProgramTargetResolver.WeekOrder)
        {
            var got = CalorieShiftCalculator.ForDay(w, CalorieShiftMode.Custom, pct, days, d);
            var factor = days.HasFlag(CalorieShiftCalculator.Flag(d)) ? 1m + pct / 100m : 1m - pct / 100m * n / (7 - n);
            var kcal = (int)Math.Round(w.Calories * factor);
            csGridValidSame &= got.Calories == kcal && got.CarbsG == Math.Round(Math.Max(0m, w.CarbsG + (kcal - w.Calories) / 4m), 1) && got.ProteinG == w.ProteinG && got.FatG == w.FatG && got.CarbsG >= 0 && got.Calories > 0 && (floor is null || got.Calories >= floor);
            sum += got.Calories;
        }
        csGridSum &= Math.Abs(sum - 7 * w.Calories) <= 4;
        continue;
    }
    csGridBad++;
    foreach (var o in CalorieShiftFeasibility.Options(w, CalorieShiftMode.Custom, pct, days, floor))
        switch (o.Code)
        {
            case CalorieShiftFeasibility.ReducePercent:
                csGridOptionsOk &= o.MaxPercent is { } csMp && csMp >= CalorieShiftCalculator.MinPercent && csMp < pct
                    && CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, csMp, days, floor).Count == 0
                    && CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, csMp + 0.01m, days, floor).Count > 0 || (o.MaxPercent is { } csMp2 && csMp2 + 0.01m >= pct);
                break;
            case CalorieShiftFeasibility.ReduceDays:
                csGridOptionsOk &= o.MaxDays is { } csMd && csMd is >= 1 && csMd < n
                    && CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, pct, CsFlags(csMd), floor).Count == 0
                    && CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, pct, CsFlags(csMd + 1), floor).Count > 0;
                break;
            case CalorieShiftFeasibility.LowerFloor:
                csGridOptionsOk &= floor is { } csFl && o.MaxFloorKcal is { } csMf && csMf < csFl
                    && CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, pct, days, csMf).Count == 0
                    && CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, pct, days, csMf + 1).Count > 0;
                break;
            case CalorieShiftFeasibility.RemoveFloor:
                csGridOptionsOk &= floor is not null && CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, pct, days, null).Count == 0;
                break;
            default: csGridOptionsOk = false; break;
        }
    // không bỏ sót: nếu 5% (hoặc 1 ngày) còn hợp lệ thì option tương ứng phải được đề xuất
    var offers = CalorieShiftFeasibility.Options(w, CalorieShiftMode.Custom, pct, days, floor);
    if (pct > CalorieShiftCalculator.MinPercent && CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, CalorieShiftCalculator.MinPercent, days, floor).Count == 0) csGridPercentOffered &= offers.Any(o => o.Code == CalorieShiftFeasibility.ReducePercent);
    if (n > 1 && CalorieShiftFeasibility.Validate(w, CalorieShiftMode.Custom, pct, CsFlags(1), floor).Count == 0) csGridDaysOffered &= offers.Any(o => o.Code == CalorieShiftFeasibility.ReduceDays);
}
Check(csGridOk >= 60 && csGridBad >= 60, $"Calorie Shift: lưới {csGridOk} tổ hợp hợp lệ + {csGridBad} không hợp lệ (đủ cả hai loại)");
Check(csGridValidSame, "Calorie Shift: tổ hợp hợp lệ giữ nguyên số của công thức cũ (kcal, carbs, protein, fat), mọi ngày > 0, ≥ sàn, carbs ≥ 0");
Check(csGridSum, "Calorie Shift: tổng tuần giữ nguyên (±4 kcal làm tròn) với mọi tổ hợp hợp lệ");
Check(csGridOptionsOk, "Calorie Shift: mọi option trả về đều hợp lệ khi áp dụng và tối đại (thêm 0.01% / thêm 1 ngày / thêm 1 kcal sàn thì không còn hợp lệ)");
Check(csGridPercentOffered && csGridDaysOffered, "Calorie Shift: không bỏ sót option khi 5% hoặc 1 ngày vẫn hợp lệ");

// Hợp đồng 400
BadRequestException? csEx = null;
try { TargetFeasibility.EnsureFeasible(TargetFeasibility.Assess(csWeek, CalorieShiftMode.Custom, 20m, CsFlags(5), 1000)); } catch (BadRequestException e) { csEx = e; }
var csModel = csEx?.Details as FeasibilityResponse;
Check(csEx is not null && csEx.StatusCode == 400 && csModel is { IsFeasible: false, ReasonCode: "CALORIE_SHIFT_INFEASIBLE", CurrentShiftPercent: 20m }
    && csModel.AffectedDays.SequenceEqual(["Saturday", "Sunday"]) && csModel.Violations.Count == 2 && csModel.Violations.All(v => v.Detail == "NEGATIVE_MACRO" && v.Code == "MACRO_BUDGET_EXCEEDED") && csEx.Errors!["targets"].Length == 2 && csModel.Actions.Contains("USE_EVEN_DISTRIBUTION"),
    "Shift 400 (định dạng DUY NHẤT FeasibilityResponse): message + errors.targets (từng ngày) + data {isFeasible, reasonCode, reasonCodes, violations, affectedDays, currentShiftPercent, actions...}; không còn CalorieShiftInfeasibleModel");
var csJson = System.Text.Json.JsonSerializer.Serialize(ApiResponse<object>.Fail(csEx!.Message, csEx.StatusCode, csEx.Errors, csEx.Details), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
Check(csJson.Contains("\"data\":{\"isFeasible\":false,\"reasonCode\":\"CALORIE_SHIFT_INFEASIBLE\"") && csJson.Contains("\"detail\":\"NEGATIVE_MACRO\"") && csJson.Contains("\"day\":\"Saturday\"") && csJson.Contains("\"warnings\":[]"),
    $"Shift 400: JSON camelCase của FeasibilityResponse ({csJson.Length} ký tự)");
Check(ApiResponse<object>.Fail("x", 400, null, null).Data is null && new BadRequestException("m").Details is null, "400 thông thường không đổi: không có data/details");
Check(CalorieShiftCalculator.ForDay(csWeek, CalorieShiftMode.Custom, 20m, CsFlags(5), DayOfWeek.Sunday) is { Calories: 1350, CarbsG: 0m }, "Calorie Shift runtime: ForDay (đường đọc target) giữ nguyên kết quả, carbs vẫn kẹp 0 — chỉ báo cáo, không tự điều chỉnh");

// ===== D1: target của ngày lịch sử theo tuần program hiệu lực NGÀY ĐÓ (kể cả program đã kết thúc) =====
MacroProgram HProg(int start, int? endedAt, bool active, params (int From, int Kcal)[] weeks)
{
    var p = new MacroProgram { Id = Guid.NewGuid(), IsActive = active, StartDate = c7Today.AddDays(start), EndedAt = endedAt is { } e ? c7Today.AddDays(e) : null, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(start) };
    foreach (var (from, kcal) in weeks) p.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), MacroProgramId = p.Id, EffectiveFrom = c7Today.AddDays(from), CaloriesTarget = kcal, ProteinTarget = 150, CarbsTarget = 200, FatTarget = 60 });
    return p;
}
int? HKcal(IReadOnlyList<MacroProgram> ps, int off) => ProgramHistory.TargetOn(ps, c7Today.AddDays(off))?.CaloriesTarget;
var hOld = HProg(-40, -21, false, (-40, 2100), (-30, 2000)); var hNew = HProg(-20, null, true, (-20, 1800), (-5, 1700));
var hPair = new[] { hOld, hNew };
Check(HKcal(hPair, -35) == 2100 && HKcal(hPair, -25) == 2000 && HKcal(hPair, -21) == 2000 && HKcal(hPair, -20) == 1800 && HKcal(hPair, -5) == 1700 && HKcal(hPair, 0) == 1700 && HKcal(hPair, -41) is null,
    "D1: ngày dưới program đã kết thúc trả target của program đó, hôm nay vẫn là program active, trước tuần đầu null");
var hEnded = new[] { HProg(-40, -11, false, (-40, 2200)) };
Check(HKcal(hEnded, -11) == 2200 && HKcal(hEnded, -10) is null && HKcal(hEnded, 0) is null, "D1: sau EndedAt không có program nào thì không có target (không kéo dài vô hạn)");
var hAdj = new[] { HProg(-30, -11, false, (-30, 2000)), HProg(-10, null, true, (-10, 1500)) };
Check(HKcal(hAdj, -12) == 2000 && HKcal(hAdj, -11) == 2000 && HKcal(hAdj, -10) == 1500, "D1: hai program liền kề chọn đúng theo ngày (−11 → cũ, −10 → mới)");
var hOverlap = new[] { HProg(-40, null, false, (-40, 2100)), HProg(-20, null, true, (-20, 1800)) };
Check(HKcal(hOverlap, -25) == 2100 && HKcal(hOverlap, -20) == 1800 && HKcal(hOverlap, 0) == 1800, "D1: program cũ chưa có EndedAt (dữ liệu trước đây) chồng lên: tuần EffectiveFrom muộn hơn thắng");
var hTie = new[] { HProg(-3, null, false, (0, 2500)), HProg(0, null, true, (0, 1700)) };
Check(HKcal(hTie, 0) == 1700 && HKcal(new[] { HProg(-3, -1, false, (0, 2500)), HProg(0, null, true, (0, 1700)) }, 0) == 1700, "D1: cùng EffectiveFrom thì program bắt đầu muộn hơn thắng; program bị thay hôm nay (EndedAt = hôm qua) không đè lên");
var hDeleted = HProg(-40, null, false, (-40, 2999)); hDeleted.IsDeleted = true;
Check(HKcal(new[] { hDeleted }, -30) is null && HKcal(new[] { hDeleted, hNew }, -30) is null, "D1: program xoá mềm không bao giờ được đọc");

// Message khoảng số: placeholder {From}/{To} của InclusiveBetween (trước đây dùng message độ dài chuỗi)
var rangeResult = new CreateProgramCommandValidator().Validate(new CreateProgramCommand(GoalType.WeightLoss, 0.5m, ProgramType.Coached, DietStyle.Balanced, ProteinLevel.Moderate, 100, null, 500));
var rangeMsgs = rangeResult.Errors.Select(e => e.ErrorMessage).ToList();
Check(rangeMsgs.Count == 2 && rangeMsgs.Any(m => m.EndsWith("must be between 800 and 3000")) && rangeMsgs.Any(m => m.EndsWith("must be between 1000 and 6000")) && rangeMsgs.All(m => !m.Contains("characters") && !m.Contains("{")),
    $"Validation: message khoảng số dùng {{From}}/{{To}} ('{string.Join("' | '", rangeMsgs)}')");

// ===== B2: /expenditure/daily tính target khi đọc (không dùng cột snapshot lưu sẵn) =====
// Snapshot lưu 999 (giá trị cũ) cho mọi ngày; program cũ đã kết thúc + program active có tuần mới EffectiveFrom −5 (như sau một lần check-in)
var dpOld = HProg(-40, -21, false, (-40, 2100), (-30, 2000)); var dpNew = HProg(-20, null, true, (-20, 1800), (-5, 1700));
var dpPrograms = new[] { dpOld, dpNew };
decimal DpKcal(int off) => DailyProgressTargets.On(dpPrograms, c7Today.AddDays(off)).Calories;
Check(DpKcal(-35) == 2100 && DpKcal(-25) == 2000 && DpKcal(-21) == 2000 && DpKcal(-20) == 1800 && DpKcal(-6) == 1800 && DpKcal(-5) == 1700 && DpKcal(0) == 1700,
    "B2: target ngày = tuần hiệu lực của ngày đó (program đã kết thúc giữ lịch sử, ngày từ tuần mới trở đi dùng target mới), không phải giá trị lưu sẵn");
Check(Enumerable.Range(-40, 41).All(o => DpKcal(o) == (HKcal(dpPrograms, o) ?? 0)), "B2: /expenditure/daily khớp Food Log (ProgramHistory.TargetOn) cho mọi ngày, 0 khi chưa có program (cùng quy ước snapshot cũ)");
var dpEndedOnly = new[] { HProg(-40, -11, false, (-40, 2200)) };
Check(DailyProgressTargets.On(dpEndedOnly, c7Today.AddDays(-11)).Calories == 2200m && DailyProgressTargets.On(dpEndedOnly, c7Today.AddDays(-10)) == (0m, null), "B2: program đã kết thúc: ngày trong khoảng có target, sau EndedAt = 0 và không có weekId");
Check(DailyProgressTargets.On(dpPrograms, c7Today.AddDays(-5)).WeekId == dpNew.Weeks.Single(w => w.EffectiveFrom == c7Today.AddDays(-5)).Id && DailyProgressTargets.On(dpPrograms, c7Today.AddDays(-6)).WeekId == dpNew.Weeks.Single(w => w.EffectiveFrom == c7Today.AddDays(-20)).Id,
    "B2: weekId là tuần hiệu lực của đúng ngày");
// ===== #1: ngày entry 0 kcal (không nhịn ăn) KHÔNG phải ngày calo hợp lệ cho thuật toán =====
FoodLogSupport.LoggedDay ZDay(int off, decimal kcal, bool incomplete = false, bool fasting = false) => new(c7Today.AddDays(off), kcal, 0, 0, 0, 0, 0, incomplete, fasting);
Check(ZDay(0, 2500).AlgorithmKcal == 2500m && ZDay(0, 0).AlgorithmKcal is null && ZDay(0, 0, fasting: true).AlgorithmKcal == 0m && ZDay(0, 2500, incomplete: true).AlgorithmKcal is null && ZDay(0, 0, incomplete: true, fasting: true).AlgorithmKcal is null,
    "#1: AlgorithmKcal: >0 hợp lệ; 0 kcal không nhịn ăn = không hợp lệ; nhịn ăn = 0 hợp lệ; chưa đủ không hợp lệ");
var zLogs = Enumerable.Range(-20, 21).Select(o => o is >= -9 and <= -7 ? ZDay(o, 0) : o == -12 ? ZDay(o, 0, fasting: true) : ZDay(o, 2500)).ToDictionary(d => d.Date);
var zWindow = Enumerable.Range(-20, 21).Select(o => new ExpenditureDay(c7Today.AddDays(o), FoodLogSupport.AlgorithmKcalOn(zLogs, c7Today.AddDays(o)), 80m)).ToList();
var zRaw = ExpenditureCalculator.ComputeRaw(zWindow, 7700);
Check(zRaw is not null && zRaw.LoggedDays == 18 && zRaw.AvgKcalIn == Math.Round(2500m * 17 / 18, 2) && zRaw.TdeeRaw == zRaw.AvgKcalIn,
    $"#1: ComputeRaw: 3 ngày 0 kcal bị bỏ, nhịn ăn hợp lệ vẫn tính 0 (loggedDays {zRaw?.LoggedDays}, avg {zRaw?.AvgKcalIn})");
Check(FoodLogSupport.AlgorithmKcalOn(zLogs, c7Today.AddDays(-30)) is null, "#1: ngày chưa log (vắng trong từ điển) = không hợp lệ");
var zLast7 = Enumerable.Range(-6, 7).Select(o => new ExpenditureDay(c7Today.AddDays(o), o <= -3 ? FoodLogSupport.AlgorithmKcalOn(new Dictionary<DateOnly, FoodLogSupport.LoggedDay> { [c7Today.AddDays(o)] = ZDay(o, 0) }, c7Today.AddDays(o)) : 2500m, 80m)).ToList();
Check(ExpenditureCalculator.HoldingReason(zLast7)?.Contains("4/7") == true && ExpenditureCalculator.Status(zLast7) == ExpenditureStatus.Holding,
    "#1: 4/7 ngày gần nhất 0 kcal (không nhịn ăn) = thiếu log ⇒ Holding");
var zEb = EnergyBalanceCalculator.Days(c7Today.AddDays(-20), c7Today, zLogs, new Dictionary<DateOnly, decimal>(), _ => null);
Check(zEb.Count(EnergyBalanceCalculator.Counts) == 18 && zEb.Single(d => d.Date == c7Today.AddDays(-8)).CaloriesKcal == 0m && !zEb.Single(d => d.Date == c7Today.AddDays(-8)).IsIncomplete && EnergyBalanceCalculator.Counts(zEb.Single(d => d.Date == c7Today.AddDays(-12))),
    "#1: Energy Balance: ngày 0 kcal hiện 0 (không phải 'chưa đủ') nhưng không vào thống kê; nhịn ăn vẫn vào");
// Q1: ngày hỗn hợp: 0 kcal (−9..−7), nhịn ăn (−12), chưa đủ (−15), chưa log (−16); expenditure thiếu ở −19..−17; target 2200
var ebMixLogs = zLogs.ToDictionary(kv => kv.Key, kv => kv.Value);
ebMixLogs[c7Today.AddDays(-15)] = ZDay(-15, 2500, incomplete: true); ebMixLogs.Remove(c7Today.AddDays(-16));
var ebMixExp = Enumerable.Range(-20, 21).Where(o => o is < -19 or > -17).ToDictionary(o => c7Today.AddDays(o), o => 2300m);
var ebMix = EnergyBalanceCalculator.Days(c7Today.AddDays(-20), c7Today, ebMixLogs, ebMixExp, _ => 2200m);
EnergyBalanceDay EbOn(int o) => ebMix.Single(d => d.Date == c7Today.AddDays(o));
Check(EbOn(-8) is { CaloriesKcal: 0m, VsExpenditureKcal: null, VsTargetKcal: null, IsFasting: false, IsIncomplete: false } && EbOn(-12) is { CaloriesKcal: 0m, VsExpenditureKcal: -2300m, VsTargetKcal: -2200m, IsFasting: true },
    "Q1: ngày 0 kcal không nhịn ăn: hiện 0 nhưng vsExpenditure/vsTarget = null; nhịn ăn vẫn là 0 kcal có thâm hụt thật (−2300)");
Check(EbOn(-15) is { CaloriesKcal: 2500m, IsIncomplete: true, VsExpenditureKcal: null, VsTargetKcal: null } && EbOn(-16) is { CaloriesKcal: null, VsExpenditureKcal: null, VsTargetKcal: null } && !EnergyBalanceCalculator.Counts(EbOn(-15)) && !EnergyBalanceCalculator.Counts(EbOn(-16)),
    "Q1: chưa đủ giữ kcal đã log + cờ nhưng vs* = null, chưa log không có số; cả hai không vào thống kê");
Check(EbOn(-18) is { CaloriesKcal: 2500m, VsExpenditureKcal: null, VsTargetKcal: 300m } && EbOn(-18).ExpenditureKcal is null,
    "Q1: ngày có kcal nhưng chưa có expenditure: vsExpenditure null, vsTarget vẫn có");
var ebMixSum = EnergyBalanceCalculator.Summary(ebMix, c7Today.AddDays(-20), c7Today);
var ebPairs = ebMix.Where(d => EnergyBalanceCalculator.Counts(d) && d.ExpenditureKcal.HasValue).ToList();
Check(ebMixSum.LoggedDays == 16 &&ebMixSum.AvgVsExpenditureKcal == Math.Round(ebPairs.Average(d => d.CaloriesKcal!.Value - d.ExpenditureKcal!.Value), 0, MidpointRounding.AwayFromZero)
    && ebMixSum.AvgExpenditureKcal == 2300m && ebMixSum.VsExpenditureDays == ebPairs.Count && ebMixSum.TotalVsExpenditureKcal == ebPairs.Sum(d => d.CaloriesKcal!.Value - d.ExpenditureKcal!.Value),
    $"Q1: thống kê EB theo CẶP ngày hợp lệ (kcal + expenditure cùng ngày): loggedDays {ebMixSum.LoggedDays}, cặp {ebPairs.Count}, avg vs {ebMixSum.AvgVsExpenditureKcal}");
Check(EnergyBalanceCalculator.Buckets(ebMix, MetricGranularity.Week, DayOfWeek.Monday).Sum(b => b.LoggedDays) == 16 &&EnergyBalanceCalculator.Relative(ebMix, c7Today, false).Single(r => r.Days == 30).LoggedDays == ebPairs.Count,
    "Q1: bucket và Relative cùng tập ngày hợp lệ (không có ngày 0 kcal / chưa đủ)");
// ===== #4: TDEE khởi điểm = Cunningham (500 + 22 × lean mass) khi hồ sơ có % mỡ; Mifflin-St Jeor khi không =====
UserProfile CunProfile(Gender g, BodyFatBand? bf, decimal height = 180m)
    => new() { Gender = g, DateOfBirth = c7Today.AddYears(-30).AddDays(-10), HeightCm = height, BodyFat = bf, DailyActivity = DailyActivityBand.ModeratelyActive, ExerciseSessions = ExerciseSessionsBand.FourToSix };   // MF: 1.4 + 0.2 = 1.6
var cunA = OnboardingCalculator.EstimateTdee(CunProfile(Gender.Male, BodyFatBand.Bf18To23), 80m, c7Today);
Check(cunA == new TdeeEstimate(3039, 1899, "Cunningham", 20.5m), $"#4: có % mỡ (20.5%) 80 kg: lean 63.6 → BMR 500 + 22 × 63.6 = 1899, TDEE × 1.6 (bảng MF: Moderate 1.4 + 4–6 buổi 0.2) = 3039 ({cunA})");
var mifA = OnboardingCalculator.EstimateTdee(CunProfile(Gender.Male, null), 80m, c7Today);
Check(mifA == new TdeeEstimate(2848, 1780, "MifflinStJeor", null), $"#4: không có % mỡ: giữ Mifflin-St Jeor dự phòng (BMR 1780, TDEE 2848), không bịa % mỡ mặc định ({mifA})");
var cunF = OnboardingCalculator.EstimateTdee(CunProfile(Gender.Female, BodyFatBand.Bf18To23, height: 160m), 80m, c7Today);
Check(cunF.TdeeKcal == cunA.TdeeKcal && cunF.BmrKcal == cunA.BmrKcal, "#4: Cunningham chỉ phụ thuộc lean mass (không phụ thuộc giới/chiều cao/tuổi)");
var cunLean = OnboardingCalculator.EstimateTdee(CunProfile(Gender.Male, BodyFatBand.Bf10To13), 70m, c7Today);
var cunHeavy = OnboardingCalculator.EstimateTdee(CunProfile(Gender.Male, BodyFatBand.Bf34To37), 120m, c7Today);
var cunTop = OnboardingCalculator.EstimateTdee(CunProfile(Gender.Male, BodyFatBand.Bf50Plus), 100m, c7Today);
Check(cunLean.BmrKcal == 1863 && cunLean.TdeeKcal == 2981 && cunHeavy.BmrKcal == 2203 && cunHeavy.TdeeKcal == 3524 && cunTop.BmrKcal == 1556 && cunTop.BodyFatPercent == 52m,
    $"#4: BMI khác nhau: gầy 70 kg/11.5% → {cunLean.BmrKcal}/{cunLean.TdeeKcal}, nặng 120 kg/35.5% → {cunHeavy.BmrKcal}/{cunHeavy.TdeeKcal}, 100 kg/52% → {cunTop.BmrKcal}");
Check(OnboardingCalculator.CunninghamBmr(0m) == 500m && OnboardingCalculator.CunninghamBmr(60m) == 1820m, "#4: CunninghamBmr(lean) = 500 + 22 × lean");
var cunInput = new ProgramPreviewInput(GoalType.Maintenance, null, 0m, DietStyle.Balanced, ProteinLevel.Moderate, CalorieFloorChoice.Standard, TrainingType.None, CalorieShiftMode.Even, null, null, 2300);
var cunPrevManual = ProgramPreviewCalculator.Compute(CunProfile(Gender.Male, BodyFatBand.Bf18To23), 80m, cunInput, 7700, c7Today);
var cunPrevAuto = ProgramPreviewCalculator.Compute(CunProfile(Gender.Male, BodyFatBand.Bf18To23), 80m, cunInput with { ManualTdeeKcal = null }, 7700, c7Today);
Check(cunPrevManual.TdeeKcal == 2300 && cunPrevAuto.TdeeKcal == 3039 && cunPrevAuto.FloorStandardKcal == 1899 && cunPrevAuto.FloorLowKcal == 1234,
    "#4: TDEE user tự nhập (2300) không bị Cunningham ghi đè; không nhập thì xem trước dùng đúng ước tính (3039), sàn kcal theo BMR Cunningham");
// ===== A7: nhãn phiên bản thuật toán (hai chuỗi tách rời; dòng chưa nhãn = Legacy, không đoán) =====
Check(AlgorithmVersions.Expenditure == "6" && AlgorithmVersions.WeightTrend == "2" && AlgorithmVersions.Legacy == "1" && AlgorithmVersions.Number(null) == 0 && AlgorithmVersions.Number("abc") == 0 && AlgorithmVersions.Number("3") == 3,
    "A7: nhãn phiên bản: Expenditure v2, Weight Trend v2 (chuỗi riêng), Legacy '1'/null/không đọc được = số 0 hoặc cũ hơn mọi nhãn");
Check(AlgorithmVersions.IsNewer("3", "2") && !AlgorithmVersions.IsNewer("2", "2") && !AlgorithmVersions.IsNewer("1", "2") && !AlgorithmVersions.IsNewer(null, "2") && AlgorithmVersions.IsNewer("2", "1"),
    "A7: IsNewer: kết quả phiên bản cũ không được ghi đè snapshot phiên bản mới hơn; Legacy luôn bị v2 thay thế");
var syncOpt = new AlgorithmOptions();
Check(syncOpt.DerivedSyncRowBudget == 300 && syncOpt.DerivedJobMaxAttempts == 5 && syncOpt.DerivedJobLeaseSeconds == 300 && syncOpt.DerivedLockWaitMs == 3000,
    "A7: mặc định vận hành: ngân sách đồng bộ 300 dòng, 5 lần thử, lease 300 s, chờ khoá 3 s");
Check(DerivedDataSync.LockResource(Guid.Parse("00000000-0000-0000-0000-000000000001")) == "macroos:derived:00000000000000000000000000000001", "A7: tên khoá theo user ổn định (nhiều instance dùng chung)");
Check(new DerivedDataStatusResponse(DerivedDataStatusResponse.Updating, null, null, 0, null).State == "Updating" && DerivedDataStatusResponse.Ready == "Ready" && DerivedDataStatusResponse.Failed == "Failed", "A7: hợp đồng status: Ready | Updating | Failed");
// ===== Ngưỡng Updating/Holding (BE là nguồn sự thật; FE đọc qua /expenditure/insights) =====
List<ExpenditureDay> Seven(int logged, int weighed) => Enumerable.Range(0, 7).Select(i => new ExpenditureDay(c7Today.AddDays(i), i < logged ? 2000m : null, i < weighed ? 80m : null)).ToList();
Check(ExpenditureCalculator.MinLoggedDaysPer7 == 4 && ExpenditureCalculator.MinWeighInDaysPer7 == 1 && ExpenditureCalculator.MaxMissingLogDays == 3,
    "Ngưỡng: ≥ 4/7 ngày log hợp lệ (tối đa 3 thiếu) và ≥ 1 lần cân trong 7 ngày");
Check(ExpenditureCalculator.Status(Seven(2, 1)) == ExpenditureStatus.Holding && ExpenditureCalculator.Status(Seven(3, 1)) == ExpenditureStatus.Holding && ExpenditureCalculator.Status(Seven(4, 1)) == ExpenditureStatus.Updating && ExpenditureCalculator.Status(Seven(7, 1)) == ExpenditureStatus.Updating,
    "Status theo số ngày log: 2/7 và 3/7 = Holding, 4/7 và 7/7 = Updating (khi có ≥ 1 lần cân)");
Check(ExpenditureCalculator.Status(Seven(7, 0)) == ExpenditureStatus.Holding && ExpenditureCalculator.Status(Seven(7, 1)) == ExpenditureStatus.Updating && ExpenditureCalculator.Status(Seven(3, 0)) == ExpenditureStatus.Holding,
    "Status theo số lần cân: 0/7 = Holding, 1/7 = Updating (khi đủ log)");
Check(ExpenditureCalculator.HoldingReason(Seven(3, 1))!.Contains("Thiếu log 4/7") && ExpenditureCalculator.HoldingReason(Seven(3, 1))!.Contains("tối đa 3") && ExpenditureCalculator.HoldingReason(Seven(4, 1)) is null,
    "Lý do Holding nêu số ngày thiếu (4/7) và trần (tối đa 3); đủ điều kiện thì không có lý do");
var thrIns = ExpenditureSeries.Insights([], c7Today, null, 14, Seven(7, 7));
Check(thrIns.MinLoggedDaysPer7 == 4 && thrIns.MinWeighInDaysPer7 == 1, "Insights trả ngưỡng minLoggedDaysPer7 = 4, minWeighInDaysPer7 = 1 (kể cả khi chưa có snapshot)");
// Chuyển trạng thái Holding → Updating (không yêu cầu liên tiếp): ngày thứ 4 có log trong 7 ngày gần nhất là đủ
var thrDays = Enumerable.Range(0, 11).Select(i => new ExpenditureDay(c7Today.AddDays(i), i >= 4 ? 2000m : null, i is 0 or 6 ? 80m : null)).ToList();
var thrStatus = Enumerable.Range(6, 5).Select(e => ExpenditureCalculator.Status(thrDays.Skip(e - 6).Take(7).ToList())).ToList();
Check(thrStatus[0] == ExpenditureStatus.Holding && thrStatus[1] == ExpenditureStatus.Updating && thrStatus[2] == ExpenditureStatus.Updating && thrStatus[4] == ExpenditureStatus.Updating,
    $"Chuyển trạng thái Holding → Updating: sau nghỉ log, ngày log thứ 4 trong cửa sổ 7 ngày (không yêu cầu liên tiếp) đưa về Updating ({string.Join(",", thrStatus)})");
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
var line = Enumerable.Range(0, 21).Select(i => new TrendPoint(Td(i), 80m - 0.02m * i, 80m - 0.02m * i)).ToList();
var ins = WeightTrendSeries.Insights(line, Td(20), N, 7700);
Check(ins.CurrentWeightKg == 79.60m && ins.WeeklyChangeKg == -0.14m && ins.EnergyDeficitKcalPerDay == -154m && ins.Projected30DaysKg == 79.60m - 0.6m
    && ins.WeightChanges.Select(c => (c.Days, c.ChangeKg, c.Status)).SequenceEqual(new (int, decimal?, string)[] { (3, -0.06m, "Decrease"), (7, -0.14m, "Decrease"), (14, -0.28m, "Decrease"), (30, null, "Waiting"), (90, null, "Waiting") }),
    $"Insights (dốc −0.02 kg/ngày): hiện tại 79.60, −0.14 kg/tuần, thâm hụt −154 kcal/ngày (×7700÷7), dự kiến 78.99 sau 30 ngày; 3/7/14 ngày có giá trị, 30/90 Waiting ({ins.WeeklyChangeKg}, {ins.EnergyDeficitKcalPerDay}, {ins.Projected30DaysKg})");
var flat = Enumerable.Range(0, 10).Select(i => new TrendPoint(Td(i), 80m - 0.01m * i, 80m - 0.01m * i)).ToList();
Check(WeightTrendSeries.Insights(flat, Td(9), N, 7700).WeightChanges[0] is { Days: 3, Status: "NoChange", ChangeKg: -0.03m } && WeightTrendSeries.Insights(flat, Td(9), N, 7700).WeightChanges[1] is { Days: 7, Status: "Decrease", ChangeKg: -0.07m },
    "Weight Changes: |thay đổi| < 0.05 kg là NoChange, từ 0.05 là Decrease");
var six = Enumerable.Range(0, 6).Select(i => new TrendPoint(Td(i), 80m - 0.02m * i, 80m - 0.02m * i)).ToList();
var seven = Enumerable.Range(0, 7).Select(i => new TrendPoint(Td(i), 80m - 0.02m * i, 80m - 0.02m * i)).ToList();
var eight = Enumerable.Range(0, 8).Select(i => new TrendPoint(Td(i), 80m - 0.02m * i, 80m - 0.02m * i)).ToList();
Check(WeightTrendSeries.Insights(six, Td(5), N, 7700).WeeklyChangeKg is null && WeightTrendSeries.Insights(seven, Td(6), N, 7700).WeeklyChangeKg is null && WeightTrendSeries.Insights(eight, Td(7), N, 7700).WeeklyChangeKg == -0.14m
    && WeightTrendSeries.Insights(seven, Td(6), N, 7700).WeightChanges[1].Status == "Waiting" && WeightTrendSeries.Insights(eight, Td(7), N, 7700).WeightChanges[1] is { Status: "Decrease", ChangeKg: -0.14m },
    "Điều kiện dữ liệu: tuần cần ≥ 7 ngày trend; 7-day change cần ngày hôm nay − 7 đã có trend (Waiting đến ngày thứ 8) như MacroFactor");
// Hồi quy chỉ dùng 20 ngày gần nhất của trend hằng ngày: 40 ngày, 19 ngày đầu dốc −0.1, phần cuối phẳng → weekly ≈ 0
var bend = Enumerable.Range(0, 40).Select(i => new TrendPoint(Td(i), i < 19 ? 80m - 0.1m * i : 80m - 0.1m * 18, i < 19 ? 80m - 0.1m * i : 80m - 0.1m * 18)).ToList();
Check(WeightTrendSeries.Insights(bend, Td(39), N, 7700).WeeklyChangeKg == 0m, "Hồi quy chỉ dùng 20 ngày gần nhất (đoạn dốc cũ không ảnh hưởng)");
// #2a: ngày thứ 21 từ cuối (i = 19 = 78.1) nằm NGOÀI cửa sổ 20 ngày; cửa sổ 21 cũ sẽ lẫn nó và cho −0.01 kg/tuần
var bend20 = Enumerable.Range(0, 40).Select(i => new TrendPoint(Td(i), i <= 19 ? 80m - 0.1m * i : 78.0m, i <= 19 ? 80m - 0.1m * i : 78.0m)).ToList();
Check(WeightTrendSeries.RegressionDays == 20 && WeightTrendSeries.Insights(bend20, Td(39), N, 7700).WeeklyChangeKg == 0m,
    "#2a: weekly rate dùng đúng 20 ngày gần nhất của trend hằng ngày (điểm thứ 21 từ cuối không ảnh hưởng)");
var win20 = Enumerable.Range(0, 20).Select(i => new TrendPoint(Td(i), 80m - 0.03m * i, 80m - 0.03m * i)).ToList();
Check(WeightTrendSeries.Insights(win20, Td(19), N, 7700).WeeklyChangeKg == -0.21m, "#2a: đúng 20 điểm đều dốc −0.03 kg/ngày → −0.21 kg/tuần (ngưỡng ≥ 7 ngày giữ nguyên)");

// --- Expenditure series: chuỗi liên tục tới hôm nay (ngày thiếu snapshot giữ TDEE), Flux, change, Insights ---
ExpenditureSnapshotRow Snap(int d, decimal tdee, string status = "Updating", int window = 21) => new(Td(d), tdee, status, window);
var snapRows = new[] { Snap(0, 2000m, "Calibrating", 3), Snap(1, 2010m, "Calibrating", 4), Snap(2, 2020m), Snap(3, 2020m, "Holding") };
var expDaily = ExpenditureSeries.Daily(snapRows, Td(7), Td(-30), 14);
Check(expDaily.Count == 8 && expDaily.Zip(expDaily.Skip(1)).All(p => p.Second.Date == p.First.Date.AddDays(1)) && expDaily[0].Date == Td(0) && expDaily[^1].Date == Td(7),
    "Expenditure series: liên tục từ snapshot đầu tới hôm nay");
Check(expDaily.Skip(4).All(p => p.Provisional && p.Tdee == 2020m && p.Status == "Holding" && p.WindowDays == 21) && expDaily.Take(4).All(p => !p.Provisional),
    "Ngày sau snapshot cuối giữ TDEE gần nhất, Holding, giữ cửa sổ dữ liệu");
Check(ExpenditureSeries.Daily(snapRows, Td(7), Td(-5), 14).Skip(4).All(p => p.Status == "Holding" && p.IsCalibrating) && ExpenditureSeries.Daily(snapRows, Td(7), null, 14).Skip(4).All(p => p.Status == "Holding" && !p.IsCalibrating)
    && ExpenditureSeries.Daily(snapRows, Td(7), Td(-30), 14).All(p => !p.IsCalibrating),
    "#3: ngày thiếu snapshot luôn Holding; nhãn IsCalibrating tách riêng (< 14 ngày từ start), không có start/quá 14 ngày: false");
var gapDaily = ExpenditureSeries.Daily([Snap(0, 2000m), Snap(3, 2100m)], Td(4), Td(-30), 14);
Check(gapDaily.Count == 5 && gapDaily[1].Tdee == 2000m && gapDaily[2].Tdee == 2000m && gapDaily[1].Provisional && gapDaily[3].Tdee == 2100m && gapDaily[4].Tdee == 2100m, "Khoảng trống giữa hai snapshot giữ TDEE ngày trước");
Check(ExpenditureSeries.Daily([], Td(5), Td(-30), 14).Count == 0 && ExpenditureSeries.Daily([Snap(9, 2000m)], Td(5), Td(-30), 14).Count == 0 && ExpenditureSeries.Daily([Snap(0, 2000m), Snap(9, 2100m)], Td(5), Td(-30), 14).Count == 6,
    "Không có snapshot / chỉ có snapshot tương lai: rỗng; snapshot sau hôm nay bị bỏ");
var toDays = ExpenditureSeries.ToDays(ExpenditureSeries.Daily([Snap(0, 2000.4m, "Updating", 21), Snap(1, 2000.8m, "Updating", 21), Snap(2, 2100m, "Calibrating", 2)], Td(2), null, 14));
Check(toDays[0] is { ExpenditureKcal: 2000m, FluxLowKcal: null, FluxHighKcal: null, ChangeKcal: null } && toDays[1] is { ExpenditureKcal: 2001m, ChangeKcal: 0m }, "ToDays: kcal làm tròn, change tính từ giá trị chưa làm tròn (0.4 → 0)");
Check(toDays.All(d => d.FluxLowKcal is null && d.FluxHighKcal is null) && toDays[2] is { ChangeKcal: 99m },
    "Flux GIỮ ẨN: chuỗi series trả fluxLowKcal / fluxHighKcal = null ở MỌI ngày (dải ± hằng số cũ đã gỡ khỏi API); TDEE/status/change không đổi");
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
Check(calIns.Strategy == "Calibrating" && calIns.CalibratingDaysLeft is null && calIns.IsCalibrating && calIns.HoldingReason is null && calIns.HoldingReasonCodes.Count == 0, "#3: Insights: IsCalibrating là nhãn riêng, không còn đếm ngược (CalibratingDaysLeft null), snapshot cũ còn status Calibrating thì giữ nguyên");
var updCal = ExpenditureSeries.Insights(ExpenditureSeries.Daily([Snap(0, 2000m, "Updating")], Td(0), Td(-5), 14), Td(0), Td(-5), 14, Last7(1, 1));
Check(updCal.Strategy == "Updating" && updCal.IsCalibrating && updCal.CalibratingDaysLeft is null && !ExpenditureSeries.Insights(rawIns, Td(39), Td(-30), 14, Last7(7, 7)).IsCalibrating, "#3: đang trong nhãn Calibrating mà thuật toán đã Updating (học từ ngày 1)");
var noneIns = ExpenditureSeries.Insights([], Td(0), null, 14, Last7(0, 0));
Check(noneIns.CurrentKcal is null && noneIns.Strategy is null && noneIns.FirstDate is null && noneIns.Changes.Count == 5 && noneIns.Changes.All(c => c.Status == "Waiting"), "Insights khi chưa có snapshot: rỗng, mọi change Waiting");

// ===== EB: mỗi tab MỘT tập ngày cho cả ba số (kcal, expenditure/target, chênh lệch) =====
var pairSum = EnergyBalanceCalculator.Summary(ebMix, c7Today.AddDays(-20), c7Today);
Check(pairSum.AvgCaloriesVsExpenditureKcal - pairSum.AvgExpenditureKcal == pairSum.AvgVsExpenditureKcal && pairSum.AvgCaloriesVsTargetKcal - pairSum.AvgTargetKcal == pairSum.AvgVsTargetKcal
    && pairSum.AvgCaloriesKcal != pairSum.AvgCaloriesVsExpenditureKcal && pairSum.VsExpenditureDays == 13 && pairSum.VsTargetDays == 16,
    $"EB pairs: tab Expenditure avgCal(cặp) {pairSum.AvgCaloriesVsExpenditureKcal} − avgExp {pairSum.AvgExpenditureKcal} = vs {pairSum.AvgVsExpenditureKcal} đúng từng đơn vị; avgCalories mọi ngày hợp lệ ({pairSum.AvgCaloriesKcal}) khác tập cặp (giữ để tương thích)");
var pairBuckets = EnergyBalanceCalculator.Buckets(ebMix, MetricGranularity.Week, DayOfWeek.Monday);
Check(pairBuckets.Count > 0 && pairBuckets.All(b => b.AvgCaloriesVsExpenditureKcal - b.AvgExpenditureKcal == b.AvgVsExpenditureKcal && b.AvgCaloriesVsTargetKcal - b.AvgTargetKcal == b.AvgVsTargetKcal),
    "EB pairs: bucket cũng đúng đồng nhất từng tab");
// ===== Cờ vận hành / cờ cô lập #3 =====
var flagOpt = new AlgorithmOptions();
Check(flagOpt.BackgroundDerivedProcessingEnabled && flagOpt.FreezeTdeeDuringCalibration && AlgorithmVersions.ExpenditureFor(flagOpt) == "6" && AlgorithmVersions.ExpenditureFor(new AlgorithmOptions { FreezeTdeeDuringCalibration = false }) == "7",
    "Cờ: mặc định chạy worker nền + job đêm; mặc định ĐÓNG BĂNG (hành vi trước #3, nhãn v2); tắt đóng băng (#3) ghi nhãn v3");
var frzSnap = new[] { Snap(0, 2000m, "Calibrating") };
var frzDaily = ExpenditureSeries.Daily(frzSnap, Td(5), Td(-2), 14, freezeDuringCalibration: true);
var noFrzDaily = ExpenditureSeries.Daily(frzSnap, Td(5), Td(-2), 14, freezeDuringCalibration: false);
Check(frzDaily.Skip(1).All(p => p.Status == "Calibrating" && p.IsCalibrating) && noFrzDaily.Skip(1).All(p => p.Status == "Holding" && p.IsCalibrating),
    "Cờ đóng băng: ngày giữ TDEE trong nhãn Calibrating mang status Calibrating (trước #3) hoặc Holding (#3)");
var frzIns = ExpenditureSeries.Insights(frzDaily, Td(5), Td(-2), 14, Seven(1, 1), freezeDuringCalibration: true);
Check(frzIns.CalibratingDaysLeft == 14 - 7 && ExpenditureSeries.Insights(noFrzDaily, Td(5), Td(-2), 14, Seven(1, 1), freezeDuringCalibration: false).CalibratingDaysLeft is null,
    "Cờ đóng băng: trước #3 còn đếm ngược (14 − 7 = 7 ngày), #3 thì calibratingDaysLeft null");
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
Check(ebDays2[2] is { CaloriesKcal: 0m, IsFasting: true, VsExpenditureKcal: -2429m, VsTargetKcal: null } && ebDays2[3] is { CaloriesKcal: 5000m, IsIncomplete: true, VsExpenditureKcal: null, VsTargetKcal: null } && !EnergyBalanceCalculator.Counts(ebDays2[3]) && EnergyBalanceCalculator.Counts(ebDays2[2]),
    "Nhịn ăn: 0 kcal tính là đã log; ngày chưa đủ giữ kcal đã log + cờ nhưng vs* = null và không được tính");
var ebSum2 = EnergyBalanceCalculator.Summary(ebDays2, Td(8), Td(12));
Check(ebSum2 is { LoggedDays: 2, AvgCaloriesKcal: 1215m, AvgVsExpenditureKcal: -1214m, TotalVsExpenditureKcal: -2429m, VsExpenditureDays: 2, AvgVsTargetKcal: 429m, VsTargetDays: 1, VsTargetStatus: "AboveTarget" },
    "Summary: ngày chưa đủ bị loại; chênh lệch target chỉ trung bình các ngày có target (1 ngày), expenditure 2 ngày");
Check(EnergyBalanceCalculator.Summary([], Td(0), Td(6)) is { LoggedDays: 0, AvgVsExpenditureKcal: null, VsExpenditureStatus: "Waiting", VsTargetStatus: "Waiting" } && EnergyBalanceCalculator.Relative([], Td(5), false).All(r => r is { AvgKcal: null, Status: "Waiting", LoggedDays: 0 }),
    "Không có ngày log nào: Summary và Relative đều Waiting");
// Trung bình làm tròn half away from zero; chênh lệch = trung bình kcal (cặp) − trung bình expenditure (cùng tập ngày) nên đồng nhất từng đơn vị đã làm tròn
Check(EnergyBalanceCalculator.Summary([new(Td(1), 1000m, 2000m, 2000m, -1000m, -1000m, false, false), new(Td(2), 1000m, 2001m, 2000m, -1001m, -1000m, false, false)], Td(0), Td(2)) is { AvgExpenditureKcal: 2001m, AvgVsExpenditureKcal: -1001m, AvgCaloriesVsExpenditureKcal: 1000m }
    && EnergyBalanceCalculator.Summary([new(Td(0), 2000m, 2000m, 2000m, 0m, 0m, false, false), new(Td(1), 2001m, 2000m, 2000m, 1m, 1m, false, false)], Td(0), Td(1)) is { AvgCaloriesVsExpenditureKcal: 2001m, AvgVsExpenditureKcal: 1m }
    && EnergyBalanceCalculator.Summary([new(Td(0), 1000m, 1001m, 2000m, -1m, -1000m, false, false), new(Td(1), 1000m, 1002m, 2000m, -2m, -1000m, false, false)], Td(0), Td(1)) is { AvgExpenditureKcal: 1002m, AvgVsExpenditureKcal: -2m },
    "Làm tròn half away from zero (−1000.5 → −1001, 2000.5 → 2001, 1001.5 → 1002) và avgVs = avgCal − avgExp trên cùng tập ngày");
var ebWeekDays = EnergyBalanceCalculator.Days(Td(0), Td(13), Enumerable.Range(0, 14).ToDictionary(i => Td(i), i => Lg(i, 2000m + i * 10)), ebExp.Concat(Enumerable.Range(21, 0).Select(i => new KeyValuePair<DateOnly, decimal>(Td(i), 0m))).ToDictionary(k => k.Key, k => k.Value), _ => 2000m);
var ebBuckets = EnergyBalanceCalculator.Buckets(ebWeekDays, MetricGranularity.Week, DayOfWeek.Monday);
Check(ebBuckets.Sum(b => b.LoggedDays) == 14 && ebBuckets.All(b => b.Start.DayOfWeek == DayOfWeek.Monday && b.End == b.Start.AddDays(6)) && ebBuckets[0].AvgCaloriesKcal == Math.Round(ebWeekDays.Where(d => d.Date >= ebBuckets[0].Start && d.Date <= ebBuckets[0].End).Average(d => d.CaloriesKcal!.Value), 0, MidpointRounding.AwayFromZero)
    && EnergyBalanceCalculator.Buckets(ebWeekDays, MetricGranularity.Day, DayOfWeek.Monday).Count == 14 && EnergyBalanceCalculator.Buckets(ebWeekDays, MetricGranularity.Month, DayOfWeek.Monday).Sum(b => b.LoggedDays) == 14,
    "Buckets Day/Week/Month: trung bình các ngày đã log trong bucket");
// Target của từng ngày: tuần program hiệu lực tại ngày đó (không phải target hiện tại)
var ebProgram = new MacroProgram { Id = Guid.NewGuid(), CheckInWeekday = DayOfWeek.Monday };
ebProgram.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = Td(0), CaloriesTarget = 2000 });
ebProgram.Weeks.Add(new MacroProgramWeek { Id = Guid.NewGuid(), EffectiveFrom = Td(10), CaloriesTarget = 2200 });
var tf = EnergyBalanceSupport.TargetFn([ebProgram]);
Check(tf(Td(-1)) is null && tf(Td(0)) == 2000m && tf(Td(9)) == 2000m && tf(Td(10)) == 2200m && tf(Td(30)) == 2200m && EnergyBalanceSupport.TargetFn([])(Td(0)) is null, "Target theo ngày: tuần cũ cho ngày cũ, tuần mới từ EffectiveFrom, trước tuần đầu / không program = null");
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
var pf = EnergyBalanceSupport.TargetFn([tfp], w => w.ProteinTarget); var ff = EnergyBalanceSupport.TargetFn([tfp], w => w.FatTarget);
Check(pf(Td(5)) == 150m && pf(Td(12)) == 170m && ff(Td(5)) == 60m && ff(Td(12)) == 70m && pf(Td(-1)) is null && EnergyBalanceSupport.TargetFn([tfp])(Td(12)) == 2200m, "Target macro theo ngày: protein/fat/carbs đổi theo tuần program hiệu lực (như calories)");

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

// --- BMR: các phương trình đã xác minh (giá trị tham chiếu tính độc lập bằng Python), điều chỉnh thích nghi, hệ số hoạt động, chọn phương trình ---
static bool BmrNear(decimal a, decimal b) => Math.Abs(a - b) <= 0.01m;
Check(BmrNear(BmrEquations.General(80m, 180m, 30, false), 1740.73m) && BmrNear(BmrEquations.General(60m, 165m, 25, true), 1268.61m), "BMR Eq1: nam 30t/180/80 = 1740.73, nữ 25t/165/60 = 1268.61");
Check(BmrNear(BmrEquations.General(75m, 175m, 70, false), 1563.07m) && BmrNear(BmrEquations.General(80m, 180m, 60, false), 1681.93m), "BMR Eq1: số hạng tuổi 70t (hệ số 4.9 sau 60) = 1563.07, đúng mốc 60t = 1681.93");
Check(BmrNear(BmrEquations.BodyComposition(63.6m, 16.4m, 30), 1776.96m) && BmrNear(BmrEquations.BodyComposition(55m, 25m, 70), 1564.16m), "BMR Eq2: FFM 63.6/FM 16.4/30t = 1776.96, FFM 55/FM 25/70t = 1564.16");
Check(BmrNear(BmrEquations.Athlete(63.6m), 1937.33m) && BmrNear(BmrEquations.Athlete(70m), 2118.42m), "BMR Eq3: 40.4·FFM^0.932 (FFM 63.6 = 1937.33, 70 = 2118.42)");
Check(BmrEquations.Cunningham(63.6m) == 1899.2m, "BMR Cunningham: 500 + 22·63.6 = 1899.2");
Check(BmrAdaptation.Factor(false, false, false) == 1m && BmrAdaptation.Factor(false, true, true) == 0.95m && BmrAdaptation.Factor(true, true, true) == 0.92m
      && BmrAdaptation.Factor(true, true, false) == 0.92m && BmrAdaptation.Factor(true, false, false) == 0.97m && BmrAdaptation.Factor(true, false, true) == 0.97m,
      "BMR adaptation: thâm hụt ×0.95; đã giảm ≥ 10% so với đỉnh: đang giảm ×0.92, ổn định/tăng ×0.97 (thay hệ số thâm hụt); không giảm, không thâm hụt = 1");
Check(BmrAdaptation.ReducedFromPeak(89.9m, 100m) && !BmrAdaptation.ReducedFromPeak(90m, 100m) && !BmrAdaptation.ReducedFromPeak(80m, 0m), "BMR adaptation: đúng 0.9×đỉnh chưa tính là đã giảm; đỉnh không hợp lệ → không");
Check(ActivityMultipliers.Total(DailyActivityBand.MostlySedentary, ExerciseSessionsBand.None) == 1.2m
      && ActivityMultipliers.Total(DailyActivityBand.ModeratelyActive, ExerciseSessionsBand.FourToSix) == 1.6m
      && ActivityMultipliers.Total(DailyActivityBand.VeryActive, ExerciseSessionsBand.SevenPlus) == 1.9m
      && ActivityMultipliers.Total(DailyActivityBand.ModeratelyActive, ExerciseSessionsBand.OneToThree) == 1.5m, "BMR activity: cơ sở 1.2/1.4/1.6 + 0/0.1/0.2/0.3 (1.2, 1.5, 1.6, 1.9)");
var selFull = BmrEquationSelector.Select(80m, 180m, 30, Gender.Male, 20m, null, false);
var selNoBf = BmrEquationSelector.Select(80m, 180m, 30, Gender.Male, null, null, false);
var selAthlete = BmrEquationSelector.Select(80m, 180m, 30, Gender.Male, 20m, 7m, false);
var selNotEnoughHours = BmrEquationSelector.Select(80m, 180m, 30, Gender.Male, 20m, 6.9m, false);
var selCunn = BmrEquationSelector.Select(80m, 180m, 30, Gender.Male, 20m, 10m, true);
Check(selFull.Kind == BmrEquationKind.BodyComposition && selFull.Bmr == BmrEquations.BodyComposition(64m, 16m, 30), "BMR select: có mỡ + tuổi → Eq2 (FFM 64, FM 16)");
Check(selNoBf.Kind == BmrEquationKind.General && selNoBf.Bmr == BmrEquations.General(80m, 180m, 30, false), "BMR select: không có thành phần cơ thể → Eq1");
Check(selAthlete.Kind == BmrEquationKind.Athlete && selAthlete.Bmr == BmrEquations.Athlete(64m) && selNotEnoughHours.Kind == BmrEquationKind.BodyComposition, "BMR select: Eq3 chỉ khi xác nhận ≥ 7 giờ/tuần (6.9 giờ → Eq2)");
Check(selCunn.Kind == BmrEquationKind.Cunningham && selCunn.Bmr == 1908m, "BMR select: user chọn Cunningham → 500 + 22·64 = 1908 (ưu tiên hơn Eq3)");
Check(BmrEquationSelector.Select(80m, 180m, 30, Gender.Male, null, 10m, false).Kind == BmrEquationKind.General, "BMR select: giờ tập nặng KHÔNG có FFM thì không dùng Eq3");
Check(BmrEquationSelector.Select(80m, 180m, 30, Gender.Male, null, null, true) is { Kind: BmrEquationKind.None, Bmr: null, Reason: not null }, "BMR select: Cunningham thiếu % mỡ → None + lý do, không bịa số");
Check(BmrEquationSelector.Select(80m, 180m, 30, Gender.Other, null, null, false).Kind == BmrEquationKind.None && BmrEquationSelector.Select(80m, null, 30, Gender.Male, null, null, false).Kind == BmrEquationKind.None
      && BmrEquationSelector.Select(80m, 180m, null, Gender.Male, null, null, false).Kind == BmrEquationKind.None && BmrEquationSelector.Select(0m, 180m, 30, Gender.Male, null, null, false).Kind == BmrEquationKind.None,
      "BMR select: thiếu giới (Other, quyết định 16 mở)/chiều cao/tuổi/cân → None");
Check(BmrEquationSelector.Select(80m, null, 30, Gender.Other, 20m, null, false).Kind == BmrEquationKind.BodyComposition, "BMR select: Eq2 không cần giới → dùng được với giới Other khi có % mỡ + tuổi");

// --- Prior Weight / adaptation theo đáp án / mode timeline ---
// --- Prior Weight: ngưỡng câu hỏi (chính sách tạm MACROOS_PROVISIONAL_POLICY) tách khỏi điều kiện thích nghi; lưu đúng số đã thấy; quy tắc bảo thủ ---
Check(PriorWeightQuestion.ExactKg(79.2m) == 88m && PriorWeightQuestion.ExactKg(79.1m) == 87.8889m && PriorWeightQuestion.ExactKg(81m) == 90m && PriorWeightQuestion.Policy == "MACROOS_PROVISIONAL_POLICY",
      "PriorWeightQuestion: ngưỡng chính xác = cân/0.9 làm tròn lên 4 chữ số (79.2 → 88.0000, 79.1 → 87.8889), nhãn chính sách tạm; không hardcode 88");
Check(PriorWeightQuestion.Display(88m, "kg") == (88m, "kg") && PriorWeightQuestion.Display(87.8889m, "kg") == (88m, "kg") && PriorWeightQuestion.Display(90m, "kg") == (90m, "kg")
      && PriorWeightQuestion.Display(87.8889m, "lb") == (194m, "lb") && PriorWeightQuestion.Display(87.8889m, null) == (88m, "kg"),
      "PriorWeightQuestion: số hiển thị làm tròn LÊN theo đơn vị (87.89 kg → 88 kg hoặc 194 lb; 90 → 90 không nhảy lên 91)");
Check(new[] { 60m, 70m, 79.1m, 79.2m, 83m, 90m, 100m, 123.45m }.All(w =>
      { var ex = PriorWeightQuestion.ExactKg(w); var (dk, _) = PriorWeightQuestion.Display(ex, "kg"); var (dl, _) = PriorWeightQuestion.Display(ex, "lb");
        return dk >= ex && PriorWeightQuestion.ToKg(dl, "lb") >= ex && ex >= w / 0.9m; }),
      "PriorWeightQuestion: số hiển thị (kg hoặc lb quy ngược về kg) luôn ≥ ngưỡng chính xác ≥ cân/0.9 (Yes với số hiển thị kéo theo điều kiện 10%)");
var pwEx = PriorWeightQuestion.ExactKg(79.2m);
Check(PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.Yes, pwEx, 88m, "kg", 79.2m) == MetabolicAdaptationEligibility.Established
      && PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.Yes, PriorWeightQuestion.ExactKg(79.1m), 194m, "lb", 79.1m) == MetabolicAdaptationEligibility.Established
      && PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.Yes, PriorWeightQuestion.ExactKg(90m), 100m, "kg", 90m) == MetabolicAdaptationEligibility.Established,
      "PriorWeightAdaptation: Yes + ngưỡng đã hiển thị ≥ cân/0.9 ⇒ Established (kg, lb, đúng biên 90 → 100)");
Check(PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.Yes, 84m, 84m, "kg", 79.2m) == MetabolicAdaptationEligibility.Unknown
      && PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.Yes, 87.8889m, 87m, "kg", 79.1m) == MetabolicAdaptationEligibility.Unknown
      && PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.Yes, PriorWeightQuestion.ExactKg(79.1m), 193m, "lb", 79.1m) == MetabolicAdaptationEligibility.Unknown
      && PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.Yes, PriorWeightQuestion.ExactKg(90m), 99.99m, "kg", 90m) == MetabolicAdaptationEligibility.Unknown,
      "PriorWeightAdaptation: ngưỡng thấp hơn cân/0.9 (ví dụ cân×1.06, số hiển thị bị làm tròn xuống, lb thiếu 1, 99.99 vs 100) ⇒ Unknown, không ×0.97");
Check(PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.Yes, null, null, null, 79.2m) == MetabolicAdaptationEligibility.Unknown && PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.Yes, pwEx, 88m, "kg", null) == MetabolicAdaptationEligibility.Unknown
      && PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.No, pwEx, 88m, "kg", 79.2m) == MetabolicAdaptationEligibility.NotApplicable && PriorWeightAdaptation.Eligibility(WeightHistoryAnswer.NotSure, pwEx, 88m, "kg", 79.2m) == MetabolicAdaptationEligibility.NotApplicable
      && PriorWeightAdaptation.Eligibility(null, null, null, null, null) == MetabolicAdaptationEligibility.NotApplicable,
      "PriorWeightAdaptation: thiếu ngưỡng/cân lúc trả lời ⇒ Unknown (không dựng lại); No/NotSure/chưa trả lời ⇒ NotApplicable (NotSure không bao giờ là Yes/No)");
var adaptBad = new List<string>();
foreach (var pe in new[] { MetabolicAdaptationEligibility.Established, MetabolicAdaptationEligibility.Unknown, MetabolicAdaptationEligibility.NotApplicable })
    foreach (var pp in new WeightTrendAnswer?[] { WeightTrendAnswer.Losing, WeightTrendAnswer.Gaining, WeightTrendAnswer.Stable, WeightTrendAnswer.NotSure, null })
    {
        var est = pe == MetabolicAdaptationEligibility.Established;
        var expectUnknown = est ? (pp == WeightTrendAnswer.Losing ? 0.92m : 0.97m) : (pp == WeightTrendAnswer.Losing ? 0.95m : 1m);
        if (BmrAdaptation.Factor(pe, pp, null) != expectUnknown) adaptBad.Add($"{pe}/{pp}/null");
        if (BmrAdaptation.Factor(pe, pp, true) != (est ? 0.92m : 0.95m)) adaptBad.Add($"{pe}/{pp}/true");
        if (BmrAdaptation.Factor(pe, pp, false) != (est ? 0.97m : 1m)) adaptBad.Add($"{pe}/{pp}/false");
    }
Check(adaptBad.Count == 0 && BmrAdaptation.Factor(MetabolicAdaptationEligibility.Established, WeightTrendAnswer.Losing, null) != 0.97m * 0.95m,
      "Adaptation: Established → 0.92 (đang giảm) | 0.97; Unknown/NotApplicable → không ×0.97 (chỉ 0.95 khi đang giảm theo proxy Prior Progress); không bao giờ 0.97×0.95" + (adaptBad.Count == 0 ? "" : " LỖI: " + string.Join(",", adaptBad)));
var pwProfile = new UserProfile { UnitPreference = UnitSystem.Metric };
PriorWeightAnswer.Apply(pwProfile, WeightHistoryAnswer.Yes, 79.2m, null, null, UnitSystem.Metric);
Check(pwProfile.WeighedMoreThanBefore == WeightHistoryAnswer.Yes && pwProfile.WeighedMoreThanBeforeAtWeightKg == 79.2m && pwProfile.WeighedMoreThanBeforeThresholdKg == 88m
      && pwProfile.WeighedMoreThanBeforeThresholdShown == 88m && pwProfile.WeighedMoreThanBeforeThresholdUnit == "kg",
      "PriorWeightAnswer: lưu đáp án + cân lúc trả lời + ngưỡng chính xác + số hiển thị/đơn vị");
PriorWeightAnswer.Apply(pwProfile, WeightHistoryAnswer.No, 79.2m, 84m, "kg", UnitSystem.Metric);
Check(pwProfile.WeighedMoreThanBeforeThresholdShown == 84m && pwProfile.WeighedMoreThanBeforeThresholdKg == 88m, "PriorWeightAnswer: số user THẤY (FE gửi) được lưu nguyên, kể cả khi khác số chính sách (không tự sửa)");
PriorWeightAnswer.Apply(pwProfile, WeightHistoryAnswer.Yes, null, null, null, UnitSystem.Imperial);
Check(pwProfile.WeighedMoreThanBeforeThresholdKg is null && pwProfile.WeighedMoreThanBeforeThresholdShown is null && pwProfile.WeighedMoreThanBeforeAtWeightKg is null, "PriorWeightAnswer: chưa có cân ⇒ không lưu ngưỡng (không bịa)");
PriorWeightAnswer.Apply(pwProfile, WeightHistoryAnswer.Yes, 79.1m, null, null, UnitSystem.Imperial);
Check(pwProfile.WeighedMoreThanBeforeThresholdShown == 194m && pwProfile.WeighedMoreThanBeforeThresholdUnit == "lb" && pwProfile.WeighedMoreThanBeforeThresholdKg == 87.8889m, "PriorWeightAnswer: mặc định hiển thị theo UnitPreference (Imperial → 194 lb) mà vẫn lưu ngưỡng chính xác theo kg");
var pwQVal = new GetPriorWeightQuestionQueryValidator();
Check(pwQVal.Validate(new GetPriorWeightQuestionQuery(null, null)).IsValid && pwQVal.Validate(new GetPriorWeightQuestionQuery(79.2m, "lb")).IsValid && !pwQVal.Validate(new GetPriorWeightQuestionQuery(10m, "kg")).IsValid && !pwQVal.Validate(new GetPriorWeightQuestionQuery(80m, "st")).IsValid,
      "PriorWeightQuestion API: weightKg 20–400, unit kg|lb");
var pwSaveVal = new SaveOnboardingBasicsCommandValidator();
SaveOnboardingBasicsCommand PwSave(decimal? shown, string? unit) => new(1, null, null, null, null, null, WeightHistoryAnswer.Yes, null, null, null, null, null, null, null, null, shown, unit);
Check(pwSaveVal.Validate(PwSave(88m, "kg")).IsValid && pwSaveVal.Validate(PwSave(null, null)).IsValid && !pwSaveVal.Validate(PwSave(88m, null)).IsValid && !pwSaveVal.Validate(PwSave(null, "kg")).IsValid && !pwSaveVal.Validate(PwSave(88m, "st")).IsValid,
      "PriorWeight save: số hiển thị và đơn vị gửi cùng nhau (kg|lb) hoặc không gửi");
var modePeriod = (DateOnly d, ExpenditureMode m, int? k) => new ExpenditureModePeriod { Id = Guid.NewGuid(), EffectiveFrom = d, Mode = m, StaticTdeeKcal = k };
var mUser = Guid.NewGuid();
var modeDay = new DateOnly(2026, 10, 10);
var mList = new[] { modePeriod(modeDay, ExpenditureMode.Static, 2400), modePeriod(modeDay.AddDays(5), ExpenditureMode.Dynamic, null), modePeriod(modeDay.AddDays(9), ExpenditureMode.Static, 2300) };
Check(ExpenditureModeTimeline.ModeAt([], modeDay) == (ExpenditureMode.Dynamic, null) && ExpenditureModeTimeline.ModeAt(mList, modeDay.AddDays(-1)) == (ExpenditureMode.Dynamic, null)
      && ExpenditureModeTimeline.ModeAt(mList, modeDay) == (ExpenditureMode.Static, 2400) && ExpenditureModeTimeline.ModeAt(mList, modeDay.AddDays(4)) == (ExpenditureMode.Static, 2400)
      && ExpenditureModeTimeline.ModeAt(mList, modeDay.AddDays(5)) == (ExpenditureMode.Dynamic, null) && ExpenditureModeTimeline.ModeAt(mList, modeDay.AddDays(30)) == (ExpenditureMode.Static, 2300),
      "ModeTimeline: không có dòng/trước dòng đầu = Dynamic; dòng có hiệu lực tới ngày trước dòng kế tiếp; không chồng nhau (Static→Dynamic→Static)");
var mSame = ExpenditureModeTimeline.Apply(mList, mUser, ExpenditureMode.Static, 2500, modeDay.AddDays(9));
Check(mSame.Count == 3 && mSame.Count(x => x.EffectiveFrom == modeDay.AddDays(9)) == 1 && mSame.Single(x => x.EffectiveFrom == modeDay.AddDays(9)).StaticTdeeKcal == 2500
      && mSame.Single(x => x.EffectiveFrom == modeDay.AddDays(9)).Id == mList[2].Id && mSame.Single(x => x.EffectiveFrom == modeDay).StaticTdeeKcal == 2400,
      "ModeTimeline: chuyển lại trong cùng ngày ghi đè đúng dòng của ngày đó (cùng Id, không thêm dòng), dòng ngày khác giữ nguyên");
var mSwitch = ExpenditureModeTimeline.Apply(mSame, mUser, ExpenditureMode.Dynamic, 2500, modeDay.AddDays(9));
Check(mSwitch.Single(x => x.EffectiveFrom == modeDay.AddDays(9)) is { Mode: ExpenditureMode.Dynamic, StaticTdeeKcal: null } && ExpenditureModeTimeline.ModeAt(mSwitch, modeDay.AddDays(9)).Mode == ExpenditureMode.Dynamic,
      "ModeTimeline: Static→Dynamic cùng ngày bỏ giá trị Static, học lại ngay từ snapshot");
Check(ExpenditureModeTimeline.Validate(ExpenditureMode.Static, 2400, modeDay, modeDay) is null && ExpenditureModeTimeline.Validate(ExpenditureMode.Dynamic, null, modeDay, modeDay) is null
      && ExpenditureModeTimeline.Validate(ExpenditureMode.Static, 2400, modeDay.AddDays(-1), modeDay) is not null && ExpenditureModeTimeline.Validate(ExpenditureMode.Static, 2400, modeDay.AddDays(1), modeDay) is not null
      && ExpenditureModeTimeline.Validate(ExpenditureMode.Static, null, modeDay, modeDay) is not null && ExpenditureModeTimeline.Validate(ExpenditureMode.Static, 0, modeDay, modeDay) is not null
      && ExpenditureModeTimeline.Validate(ExpenditureMode.Dynamic, 2000, modeDay, modeDay) is not null,
      "ModeTimeline: chỉ hiệu lực từ hôm nay local (không quá khứ/tương lai); Static cần > 0; Dynamic không mang giá trị");

// --- Đồng hồ user (một định nghĩa hôm nay theo múi giờ hồ sơ): UTC+7 / UTC−5 gần nửa đêm, DST, năm nhuận, ranh giới tuần/tháng/năm, thiếu/sai múi giờ, đổi múi giờ ---
UserLocale Utc(string tz, string iso, DayOfWeek ws = DayOfWeek.Monday) => UserLocale.At(tz, DateTimeOffset.Parse(iso, System.Globalization.CultureInfo.InvariantCulture), ws);
var vn1 = Utc("Asia/Ho_Chi_Minh", "2026-03-10T16:59:00Z");
var vn2 = Utc("Asia/Ho_Chi_Minh", "2026-03-10T17:00:00Z");
Check(vn1.Today == new DateOnly(2026, 3, 10) && vn1.Time == new TimeOnly(23, 59) && vn2.Today == new DateOnly(2026, 3, 11) && vn2.Time == new TimeOnly(0, 0) && vn1.Fallback is null && vn1.ZoneId == "Asia/Ho_Chi_Minh",
      "UserClock: UTC+7 quanh nửa đêm local: 16:59Z = 10/3 23:59 local, 17:00Z = 11/3 00:00 local (không phải ngày UTC)");
var co1 = Utc("America/Bogota", "2026-03-11T04:59:00Z");
var co2 = Utc("America/Bogota", "2026-03-11T05:00:00Z");
Check(co1.Today == new DateOnly(2026, 3, 10) && co1.Time == new TimeOnly(23, 59) && co2.Today == new DateOnly(2026, 3, 11),
      "UserClock: UTC−5 quanh nửa đêm local: 04:59Z vẫn là 10/3 local (ngày UTC đã là 11/3), 05:00Z mới sang 11/3");
var ny1 = Utc("America/New_York", "2026-03-08T06:59:00Z"); var ny2 = Utc("America/New_York", "2026-03-08T07:00:00Z");
var ny3 = Utc("America/New_York", "2026-11-01T05:30:00Z"); var ny4 = Utc("America/New_York", "2026-11-01T06:30:00Z");
Check(ny1.Time == new TimeOnly(1, 59) && ny2.Time == new TimeOnly(3, 0) && ny1.Today == ny2.Today && ny3.Time == new TimeOnly(1, 30) && ny4.Time == new TimeOnly(1, 30) && ny3.Today == new DateOnly(2026, 11, 1) && ny4.Today == ny3.Today,
      "UserClock: DST: mùa xuân nhảy 01:59 → 03:00 cùng ngày; mùa thu giờ 01:30 lặp hai lần vẫn cùng ngày local (không sinh ngày sai)");
Check(Utc("Asia/Ho_Chi_Minh", "2026-12-31T17:00:00Z").Today == new DateOnly(2027, 1, 1) && Utc("Asia/Ho_Chi_Minh", "2026-02-28T17:30:00Z").Today == new DateOnly(2026, 3, 1)
      && Utc("America/Bogota", "2027-01-01T04:00:00Z").Today == new DateOnly(2026, 12, 31) && Utc("Asia/Ho_Chi_Minh", "2024-02-28T17:00:00Z").Today == new DateOnly(2024, 2, 29),
      "UserClock: ranh giới năm/tháng/năm nhuận theo ngày local (31/12 UTC+7 sang 1/1; 28/2 sang 1/3; 2024 có 29/2)");
Check(Utc("Asia/Ho_Chi_Minh", "2026-03-15T20:00:00Z").Today.DayOfWeek == DayOfWeek.Monday && DateOnly.FromDateTime(DateTimeOffset.Parse("2026-03-15T20:00:00Z", System.Globalization.CultureInfo.InvariantCulture).UtcDateTime).DayOfWeek == DayOfWeek.Sunday,
      "UserClock: thứ check-in theo ngày local (Chủ nhật 20:00 UTC là thứ Hai ở UTC+7), khác thứ theo UTC");
Check(Utc(null!, "2026-03-10T17:30:00Z") is { Fallback: "Missing", ZoneId: "UTC" } && Utc("  ", "2026-03-10T17:30:00Z").Fallback == "Missing" && Utc("Mars/Olympus", "2026-03-10T17:30:00Z") is { Fallback: "Invalid", ZoneId: "UTC" }
      && Utc("Mars/Olympus", "2026-03-10T17:30:00Z").Today == new DateOnly(2026, 3, 10),
      "UserClock: múi giờ thiếu/sai ⇒ dự phòng UTC CÓ CỜ rõ ràng (Missing/Invalid), không âm thầm coi là hợp lệ");
Check(Utc("Asia/Ho_Chi_Minh", "2026-03-10T20:00:00Z").Today != Utc("America/Bogota", "2026-03-10T20:00:00Z").Today && UserLocale.ResolveZone("Asia/Ho_Chi_Minh").Fallback is null && UserLocale.ResolveZone("Mars/Olympus").Fallback == "Invalid",
      "UserClock: đổi múi giờ hồ sơ đổi ngày local ở cùng một thời điểm (có hiệu lực ngay lần tính kế tiếp); id sai bị nhận diện để từ chối khi lưu");
Check(MacroTargetCalculator.AgeYears(new DateOnly(2000, 2, 29), new DateOnly(2026, 2, 28)) == 25 && MacroTargetCalculator.AgeYears(new DateOnly(2000, 2, 29), new DateOnly(2026, 3, 1)) == 26
      && MacroTargetCalculator.AgeYears(new DateOnly(2000, 2, 29), new DateOnly(2028, 2, 29)) == 28 && MacroTargetCalculator.AgeYears(new DateOnly(2008, 3, 11), new DateOnly(2026, 3, 10)) == 17 && MacroTargetCalculator.AgeYears(new DateOnly(2008, 3, 11), new DateOnly(2026, 3, 11)) == 18,
      "Tuổi: sinh 29/2 (năm nhuận) tính đúng; đủ 18 tuổi đúng ngày LOCAL (UTC+7: 11/3 00:00 local đủ 18 dù ngày UTC còn 10/3)");
var dobVal = new SaveOnboardingBasicsCommandValidator();
SaveOnboardingBasicsCommand DobCmd(DateOnly d) => new(1, null, d, null, null, null, null, null, null, null, null, null, null, null, null);
Check(dobVal.Validate(DobCmd(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30))).IsValid && !dobVal.Validate(DobCmd(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-10))).IsValid,
      "DOB validator: dung sai +1 ngày UTC cho múi giờ phía đông; ≥ 18 tuổi hợp lệ, 10 tuổi bị chặn (kiểm đúng theo ngày local ở handler)");
Check(System.Text.Json.JsonSerializer.Serialize(new DateOnly(2026, 3, 11)) == "\"2026-03-11\"" && System.Text.Json.JsonSerializer.Deserialize<DateOnly>("\"2026-03-11\"") == new DateOnly(2026, 3, 11),
      "FE/BE: DateOnly serialize/deserialize dạng yyyy-MM-dd, không dịch ngày theo múi giờ");

// --- Bộ tính BMR/TDEE khởi điểm dùng chung: bảng hoạt động MF, thiếu đầu vào, Gender Other, BmrSex, tuổi theo ngày local ---
var icToday = new DateOnly(2026, 6, 15);
InitialExpenditureInput IcIn(Gender? g = Gender.Male, BiologicalSex? sex = null, BodyFatBand? bf = null, DailyActivityBand? da = DailyActivityBand.MostlySedentary, ExerciseSessionsBand? ex = ExerciseSessionsBand.None, DateOnly? dob = null, decimal? h = 180m, DateOnly? today = null)
    => new(80m, dob ?? new DateOnly(1996, 6, 15), h, g, sex, bf, da, ex, today ?? icToday);
var icBase = InitialExpenditureCalculator.Compute(IcIn());   // Mifflin nam 30 tuổi, 180 cm, 80 kg: BMR 1780
Check(icBase.BmrKcal == 1780 && icBase.TdeeKcal == 2136 && icBase.ActivityMultiplier == 1.2m && icBase.Method == "MifflinStJeor" && icBase.Missing.Count == 0, "InitialCalc: bảng MF: Mostly Sedentary + 0 buổi ⇒ 1.2 (BMR 1780 → 2136)");
Check(InitialExpenditureCalculator.Compute(IcIn(da: DailyActivityBand.VeryActive, ex: ExerciseSessionsBand.SevenPlus)).ActivityMultiplier == 1.9m
      && InitialExpenditureCalculator.Compute(IcIn(da: DailyActivityBand.ModeratelyActive, ex: ExerciseSessionsBand.OneToThree)).ActivityMultiplier == 1.5m
      && InitialExpenditureCalculator.Compute(IcIn(da: DailyActivityBand.MostlySedentary, ex: ExerciseSessionsBand.FourToSix)).ActivityMultiplier == 1.4m,
      "InitialCalc: cộng 1.2/1.4/1.6 + 0/0.1/0.2/0.3 (1.9, 1.5, 1.4) thay cho bảng cộng chỉ số cũ");
var icNoAct = InitialExpenditureCalculator.Compute(IcIn(da: null, ex: null));
Check(!icNoAct.IsComputable && icNoAct.TdeeKcal is null && icNoAct.Missing.SequenceEqual(["DailyActivity", "ExerciseSessions"]), "InitialCalc: chưa trả lời Activity/Exercise ⇒ KHÔNG tính (không dùng ActivityLevel mặc định)");
var icOther = InitialExpenditureCalculator.Compute(IcIn(g: Gender.Other));
Check(!icOther.IsComputable && icOther.Missing.Contains("Sex"), "InitialCalc: Gender Other không BmrSex và không body fat ⇒ thiếu Sex (KHÔNG coi là nữ, không lấy trung bình)");
var icOtherSex = InitialExpenditureCalculator.Compute(IcIn(g: Gender.Other, sex: BiologicalSex.Female));
Check(icOtherSex.IsComputable && icOtherSex.BmrKcal == 1614 && InitialExpenditureCalculator.Compute(IcIn(g: Gender.Female)).BmrKcal == 1614 && InitialExpenditureCalculator.Compute(IcIn(g: Gender.Female, sex: BiologicalSex.Male)).BmrKcal == 1780,
      "InitialCalc: BmrSex user xác nhận được dùng (Other + Female = 1614; BmrSex thắng Gender; Female không BmrSex dùng Gender)");
var icOtherBf = InitialExpenditureCalculator.Compute(IcIn(g: Gender.Other, bf: BodyFatBand.Bf24To28, dob: null, h: null));
Check(icOtherBf.IsComputable && icOtherBf.Method == "Cunningham" && icOtherBf.BodyFatPercent == 26m, "InitialCalc: có body fat ⇒ Cunningham không cần giới/tuổi/chiều cao (đường cũ giữ nguyên cho tới khi chính sách chọn được duyệt)");
Check(InitialExpenditureCalculator.Compute(new InitialExpenditureInput(80m, null, 180m, Gender.Male, null, null, DailyActivityBand.MostlySedentary, ExerciseSessionsBand.None, icToday)).Missing.Contains("DateOfBirth") && InitialExpenditureCalculator.Compute(IcIn(h: null)).Missing.Contains("HeightCm"), "InitialCalc: thiếu DOB/chiều cao ⇒ báo thiếu, không dùng mặc định");
// Tuổi theo ngày LOCAL: sinh nhật 15/6; 14/6 local chưa đủ tuổi, 15/6 local đã sang tuổi (khác ngày UTC quanh nửa đêm UTC+7)
Check(InitialExpenditureCalculator.Compute(IcIn(today: new DateOnly(2026, 6, 14))).BmrKcal == 1785 && InitialExpenditureCalculator.Compute(IcIn(today: new DateOnly(2026, 6, 15))).BmrKcal == 1780,
      "InitialCalc: tuổi tính theo ngày local do caller truyền (29 tuổi 14/6 → BMR 1785; 30 tuổi 15/6 → 1780), không theo UTC");
Check(InitialExpenditureCalculator.Compute(IcIn()).TdeeKcal == InitialExpenditureCalculator.Compute(IcIn()).TdeeKcal && InitialExpenditureCalculator.Compute(new InitialExpenditureInput(80m, new DateOnly(1996, 6, 15), 180m, Gender.Male, null, null, DailyActivityBand.MostlySedentary, ExerciseSessionsBand.None, icToday, 0.97m)).TdeeKcal == (int)Math.Round(1780m * 0.97m * 1.2m),
      "InitialCalc: hệ số thích nghi (nếu caller truyền sau khi được duyệt) nhân MỘT lần; mặc định 1; không đụng expenditure đã học");
// Preview = Create: cùng hàm kcal/macro, sàn chỉ áp Coached
var pcCoached = ProgramWeekBuilder.ComputeTargets(2000, 1.5m, 70m, 7700, 1700, ProgramType.Coached, 1.8m, null, DietStyle.Balanced);
var pcCollab = ProgramWeekBuilder.ComputeTargets(2000, 1.5m, 70m, 7700, 1700, ProgramType.Collaborative, 1.8m, null, DietStyle.Balanced);
Check(pcCoached.Calories == 1700 && pcCollab.Calories < 1700, "Preview=Create: sàn calo chỉ áp cho Coached (cùng hàm ComputeTargets cho Preview/Create/UpdateGoal/SetManual)");
var pvProfile = new UserProfile { Gender = Gender.Male, DateOfBirth = new DateOnly(1996, 6, 15), HeightCm = 180m, DailyActivity = DailyActivityBand.MostlySedentary, ExerciseSessions = ExerciseSessionsBand.None };
var pvInput = new ProgramPreviewInput(GoalType.WeightLoss, null, 1.5m, DietStyle.Balanced, ProteinLevel.Moderate, CalorieFloorChoice.Standard, TrainingType.None, CalorieShiftMode.Even, null, null, 2000);
var pvCoached = ProgramPreviewCalculator.Compute(pvProfile, 70m, pvInput, 7700, icToday);
var pvCollab = ProgramPreviewCalculator.Compute(pvProfile, 70m, pvInput with { ProgramType = ProgramType.Collaborative }, 7700, icToday);
Check(pvCoached.AverageCalories == pvCoached.FloorStandardKcal && pvCoached.Days.All(d => d.Calories >= pvCoached.FloorStandardKcal) && pvCollab.AverageCalories < pvCoached.AverageCalories
      && ProgramPreviewCalculator.Floors(null) == (1200, 800),
      "Preview=Create: Preview dùng cùng công thức (Coached áp sàn, Collaborative không); không có BMR (thiếu câu trả lời) vẫn tính được khi có TDEE đã chọn, sàn rơi về 1200/800");

// --- Máy trạng thái Updating/Holding/Calibrating (G0, có trạng thái; MF: 4/7 + 1/7 duy trì, 3 ngày để resume; kết hợp = MacroOS policy) ---
var smStart = new DateOnly(2026, 3, 1);
// mẫu: ký tự cho từng ngày: L log hợp lệ, W log hợp lệ + cân, w chỉ cân, . không có gì
List<ExpenditureDay> SmDays(string pattern, int endIndex) => Enumerable.Range(0, endIndex + 1).Select(i => new ExpenditureDay(smStart.AddDays(i),
    pattern[i] is 'L' or 'W' ? 2000m : null, pattern[i] is 'W' or 'w' ? 80m : null)).ToList();
List<ExpenditureStatus> SmSeq(string pat, DateOnly? today = null)
{
    var list = new List<ExpenditureStatus>(); ExpenditureStatus? st = null;
    for (var i = 0; i < pat.Length; i++) { st = ExpenditureStatusMachine.Next(st, SmDays(pat, i).TakeLast(8).ToList(), today ?? smStart.AddDays(999)); list.Add(st.Value); }
    return list;
}
ExpenditureStatus SmRun(string pat) => SmSeq(pat)[^1];
Check(SmRun("L") == ExpenditureStatus.Calibrating && SmRun("LL.W") == ExpenditureStatus.Calibrating && SmRun("W") == ExpenditureStatus.Calibrating,
      "StatusMachine: chuỗi mới chưa đủ dữ liệu = Calibrating (không phải từng Updating rồi Holding)");
Check(SmRun("LLW") == ExpenditureStatus.Updating && SmRun("WLL") == ExpenditureStatus.Updating, "StatusMachine: 3 ngày liên tiếp + cân ⇒ Updating (khởi đầu/resume không cần 4/7)");
Check(SmRun("L.L.W") == ExpenditureStatus.Calibrating && SmRun("L.L.L") == ExpenditureStatus.Calibrating, "StatusMachine: 3 ngày KHÔNG liên tiếp (logged7 < 4) ⇒ không resume");
Check(SmRun("LL.LLw") == ExpenditureStatus.Updating, "StatusMachine: 4/7 log + 1 cân ⇒ Updating");
Check(SmRun("LLLL") == ExpenditureStatus.Calibrating && SmRun("LLLLw") == ExpenditureStatus.Updating, "StatusMachine: thiếu cân ⇒ không Updating dù đủ log; một cân mới ⇒ Updating ngay");
var u2h = SmSeq("WLLL....");
Check(u2h[3] == ExpenditureStatus.Updating && u2h[6] == ExpenditureStatus.Updating && u2h[7] == ExpenditureStatus.Holding, "StatusMachine: Updating → Holding khi logged7 tụt dưới 4 (ngày hoàn tất thiếu)");
var h2u = SmSeq("WLLL.....LLW");
Check(h2u[8] == ExpenditureStatus.Holding && h2u[10] == ExpenditureStatus.Holding && h2u[11] == ExpenditureStatus.Updating, "StatusMachine: Holding → Updating đúng ở ngày hợp lệ thứ 3 liên tiếp (logged7 mới 3, chưa đạt 4/7)");
var noOsc = SmSeq("WLLL.....LLWL.LL");
Check(noOsc[11] == ExpenditureStatus.Updating && noOsc[12] == ExpenditureStatus.Updating && noOsc[13] == ExpenditureStatus.Updating && noOsc[14] == ExpenditureStatus.Updating,
      "StatusMachine: không dao động sau resume: log 4 ngày rồi bỏ 1 ngày vẫn Updating (4/7 duy trì)");
Check(SmSeq("WLLL.....LL.")[11] == ExpenditureStatus.Holding, "StatusMachine: 2 ngày liên tiếp rồi bỏ ⇒ vẫn Holding (không resume)");
// Hôm nay chưa xong
var pendUpd = ExpenditureStatusMachine.Next(ExpenditureStatus.Updating, SmDays("WLLLLLLLLLL.", 11).TakeLast(8).ToList(), smStart.AddDays(11));
Check(pendUpd == ExpenditureStatus.Updating, "StatusMachine: hôm nay chưa log (chưa xong) KHÔNG làm Updating → Holding (không tính là thiếu)");
Check(ExpenditureStatusMachine.Next(ExpenditureStatus.Updating, SmDays("WLLL.LL.....", 11).TakeLast(8).ToList(), smStart.AddDays(999)) == ExpenditureStatus.Holding
      && ExpenditureStatusMachine.Next(ExpenditureStatus.Updating, SmDays("WLLL.LL.....", 11).TakeLast(8).ToList(), smStart.AddDays(11)) == ExpenditureStatus.Updating,
      "StatusMachine: cùng dữ liệu, ngày đã hoàn tất ⇒ Holding; ngày hôm nay chưa xong ⇒ giữ Updating");
Check(ExpenditureStatusMachine.Next(ExpenditureStatus.Holding, SmDays("WLL.........", 11).TakeLast(8).ToList(), smStart.AddDays(11)) == ExpenditureStatus.Holding, "StatusMachine: Holding + hôm nay chưa có gì ⇒ vẫn Holding");
Check(ExpenditureStatusMachine.Next(ExpenditureStatus.Holding, SmDays("....LLL.....", 10).TakeLast(8).ToList(), smStart.AddDays(10)) == ExpenditureStatus.Holding,
      "StatusMachine: 3 ngày liên tiếp nhưng KHÔNG có cân trong 7 ngày ⇒ chưa resume");
Check(ExpenditureStatusMachine.Next(ExpenditureStatus.Holding, SmDays("....LLLL..w", 10).TakeLast(8).ToList(), smStart.AddDays(10)) == ExpenditureStatus.Updating,
      "StatusMachine: Holding + 4 ngày hợp lệ gần đây + cân hôm nay (chưa log) ⇒ resume ngay khi có dữ liệu mới hôm nay (không đóng băng trạng thái hôm qua)");
var sm1 = ExpenditureStatusMachine.Metrics(SmDays("....wLL", 6), smStart.AddDays(999));
Check(sm1.Run == 2 && sm1.Logged7 == 2 && sm1.WeighIns7 == 1 && ExpenditureStatusMachine.ResumeInDays(sm1) == 1 && ExpenditureStatusMachine.HoldingReasonCodes(sm1).SequenceEqual(["MissingLogs", "RecoveringLogs"]),
      "StatusMachine API: 2 ngày liên tiếp + cân ⇒ resumeInDays = 1 và RecoveringLogs (ước tính nếu tiếp tục log hợp lệ, không phải cam kết)");
var sm2 = ExpenditureStatusMachine.Metrics(SmDays("LLL....", 6), smStart.AddDays(999));
var sm3 = ExpenditureStatusMachine.Metrics(SmDays("....LLL", 6), smStart.AddDays(999));
Check(ExpenditureStatusMachine.ResumeInDays(sm2) is null && ExpenditureStatusMachine.HoldingReasonCodes(sm2).Contains("NoRecentWeighIn") && ExpenditureStatusMachine.HoldingReason(sm3) is not null,
      "StatusMachine API: thiếu cân ⇒ resumeInDays null + NoRecentWeighIn");
// Nhịn ăn hợp lệ nằm trong run; ngày KcalIn null (Partial/0 kcal không nhịn ăn/chưa log) cắt run
Check(ExpenditureStatusMachine.Metrics(new List<ExpenditureDay> { new(smStart, 0m, 80m), new(smStart.AddDays(1), 0m, null), new(smStart.AddDays(2), 1800m, null) }, smStart.AddDays(999)).Run == 3
      && ExpenditureStatusMachine.Metrics(new List<ExpenditureDay> { new(smStart, 1800m, 80m), new(smStart.AddDays(1), null, null), new(smStart.AddDays(2), 1800m, null) }, smStart.AddDays(999)).Run == 1,
      "StatusMachine: nhịn ăn hợp lệ (0 kcal có KcalIn) tính vào run; ngày KcalIn null (Partial/0 kcal không nhịn ăn/chưa log) cắt run, chưa log ≠ 0 kcal");
// Ngày local (UTC+7): ngày hôm nay chưa xong theo localToday do caller truyền
var d10 = new DateOnly(2026, 3, 10);
Check(ExpenditureStatusMachine.Metrics(new List<ExpenditureDay> { new(d10.AddDays(-1), 1800m, 80m), new(d10, null, null) }, d10).TodayPending
      && !ExpenditureStatusMachine.Metrics(new List<ExpenditureDay> { new(d10.AddDays(-1), 1800m, 80m), new(d10, null, null) }, d10.AddDays(1)).TodayPending,
      "StatusMachine: hôm nay chưa xong dùng ngày local do caller truyền (UserLocale theo múi giờ hồ sơ, ví dụ UTC+7), không lấy UTC");
// Replay theo thứ tự ngày: sửa dữ liệu lùi ngày đổi các trạng thái sau đó
var beforeEdit = SmSeq("WLLL....wL.L"); var afterEdit = SmSeq("WLLL....wLLL");
Check(beforeEdit[11] == ExpenditureStatus.Holding && afterEdit[11] == ExpenditureStatus.Updating, "StatusMachine: replay theo thứ tự ngày sau khi sửa dữ liệu lùi ngày đổi đúng các trạng thái sau đó");
Check(ExpenditureCalculator.MinLoggedDaysPer7 == 4 && ExpenditureCalculator.MinWeighInDaysPer7 == 1 && ExpenditureStatusMachine.ResumeConsecutiveDays == 3, "StatusMachine: hằng số 4/7, 1/7, 3 ngày (MF xác nhận cơ chế; định nghĩa consistent = MacroOS)");

// --- Initial goal correction (VERIFIED công thức; chưa nối) ---
Check(InitialGoalCorrection.Factor(-0.01m) == 0.96m && InitialGoalCorrection.Factor(0.005m) == 1.02m && InitialGoalCorrection.Factor(0m) == 1m && InitialGoalCorrection.Factor(-0.0075m) == 0.97m,
      "GoalCorrection: ×(1 + 4 × tỉ lệ/tuần có dấu): −1%/tuần ⇒ ×0.96, +0.5% ⇒ ×1.02, 0 ⇒ ×1, −0.75% ⇒ ×0.97 (giảm cân là âm)");
Check(InitialGoalCorrection.FactorFromProgramPercent(1m) == 0.96m && InitialGoalCorrection.FactorFromProgramPercent(-0.5m) == 1.02m && InitialGoalCorrection.FactorFromProgramPercent(0m) == 1m,
      "GoalCorrection: quy ước MacroOS (% dương = giảm) đổi dấu đúng, giảm cân làm TDEE điều chỉnh xuống");
Check(Math.Round(2500m * InitialGoalCorrection.FactorFromProgramPercent(1m) * 0.97m, 2) == 2328m && InitialGoalCorrection.FactorFromProgramPercent(1m) * 0.97m != InitialGoalCorrection.FactorFromProgramPercent(1m),
      "GoalCorrection: hệ số độc lập, tích với ×0.97 là việc của policy được duyệt (không tự cộng dồn trong hàm)");
Check(ExpenditureLabels.AlgorithmName == "MacroOS Adaptive Expenditure" && !ExpenditureLabels.AlgorithmName.Contains("V3") && new ExpenditureInsightsResponse(default, null, null, null, null, [], null, 0, 0, []).AlgorithmName == ExpenditureLabels.AlgorithmName,
      "Label: mọi GET expenditure mang nhãn 'MacroOS Adaptive Expenditure', không gọi là V3/MacroFactor");
var updVal2 = new UpdateExpenditureSettingsCommandValidator();
Check(updVal2.Validate(new UpdateExpenditureSettingsCommand(null, null, null, null, null, WeightHistoryAnswer.NotSure)).IsValid && !updVal2.Validate(new UpdateExpenditureSettingsCommand(null, null, null, null, null, (WeightHistoryAnswer)9)).IsValid,
      "Settings API: PUT weighedMoreThanBefore nhận Yes/No/NotSure, ngoài enum bị từ chối");

// --- Expenditure Settings API: validator (không DB) ---
var updVal = new UpdateExpenditureSettingsCommandValidator();
var modeVal = new SetExpenditureModeCommandValidator();
Check(updVal.Validate(new UpdateExpenditureSettingsCommand(null, null, null, null, null)).IsValid
      && AthleticPursuitAllValid(updVal) && !updVal.Validate(new UpdateExpenditureSettingsCommand(null, null, null, null, (AthleticPursuit)99)).IsValid,
      "Settings API: đúng 4 đáp án Athletic Pursuits hợp lệ, giá trị ngoài enum bị từ chối, không gửi gì cũng hợp lệ (không fallback khi chưa trả lời)");
static bool AthleticPursuitAllValid(UpdateExpenditureSettingsCommandValidator v) => Enum.GetValues<AthleticPursuit>().All(a => v.Validate(new UpdateExpenditureSettingsCommand(null, null, null, null, a)).IsValid);
Check(updVal.Validate(new UpdateExpenditureSettingsCommand(InitialEstimateMode.Manual, 2400, null, null, null)).IsValid
      && !updVal.Validate(new UpdateExpenditureSettingsCommand(InitialEstimateMode.Manual, 999, null, null, null)).IsValid && !updVal.Validate(new UpdateExpenditureSettingsCommand(InitialEstimateMode.Manual, 6001, null, null, null)).IsValid
      && !updVal.Validate(new UpdateExpenditureSettingsCommand(InitialEstimateMode.Formula, 2400, null, null, null)).IsValid && updVal.Validate(new UpdateExpenditureSettingsCommand(InitialEstimateMode.Formula, null, null, null, null)).IsValid
      && !updVal.Validate(new UpdateExpenditureSettingsCommand((InitialEstimateMode)9, null, null, null, null)).IsValid && !updVal.Validate(new UpdateExpenditureSettingsCommand(null, null, null, (BiologicalSex)9, null)).IsValid,
      "Settings API: Manual 1000–6000, Formula không mang giá trị tay, enum ngoài phạm vi bị từ chối (BmrSex chỉ Male/Female, không có Other)");
Check(modeVal.Validate(new SetExpenditureModeCommand(ExpenditureMode.Static, 2400)).IsValid && modeVal.Validate(new SetExpenditureModeCommand(ExpenditureMode.Dynamic, null)).IsValid
      && !modeVal.Validate(new SetExpenditureModeCommand(ExpenditureMode.Static, null)).IsValid && !modeVal.Validate(new SetExpenditureModeCommand(ExpenditureMode.Static, 500)).IsValid
      && !modeVal.Validate(new SetExpenditureModeCommand(ExpenditureMode.Dynamic, 2400)).IsValid,
      "Settings API: Static bắt buộc giá trị 1000–6000, Dynamic không mang giá trị");

// --- Schema S25 (KHÔNG áp dụng, không kết nối DB): ánh xạ EF, khoá/chỉ mục, nullability, migration chỉ thêm ---
var s25Opts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=127.0.0.1,1;Database=never;Connect Timeout=1").Options;
using (var s25 = new ApplicationDbContext(s25Opts))
{
    var model = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>(s25).Model;
    var ues = model.FindEntityType(typeof(UserExpenditureSettings))!;
    var emp = model.FindEntityType(typeof(ExpenditureModePeriod))!;
    var snap = model.FindEntityType(typeof(ExpenditureSnapshot))!;
    var prof = model.FindEntityType(typeof(UserProfile))!;
    Check(ues.GetTableName() == "UserExpenditureSettings" && ues.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(["UserId"]))
          && ues.GetForeignKeys().Single().DeleteBehavior == Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade && ues.GetForeignKeys().Single().PrincipalEntityType.ClrType == typeof(User),
          "S25 schema: UserExpenditureSettings UNIQUE(UserId), FK → Users cascade, KHÔNG phụ thuộc MacroProgram");
    Check(ues.FindProperty("InitialEstimateMode")!.IsNullable && ues.FindProperty("ManualInitialTdeeKcal")!.IsNullable && ues.FindProperty("CalculationStartDate")!.IsNullable
          && !ues.FindProperty("BmrEquation")!.IsNullable && !ues.FindProperty("CalculationRevision")!.IsNullable && ues.FindProperty("RowVersion")!.IsConcurrencyToken
          && new UserExpenditureSettings() is { InitialEstimateMode: null, ManualInitialTdeeKcal: null, CalculationStartDate: null, CalculationRevision: 0, BmrEquation: BmrEquationPreference.MacroFactor },
          "S25 schema: dòng tạo lười toàn NULL/mặc định (chưa cấu hình ⇒ KHÔNG ghi đè TDEE nhập tay/đã học bằng dữ liệu program cũ); RowVersion là concurrency token");
    Check(ues.GetCheckConstraints().Select(c => c.Name).Order().SequenceEqual(["CK_UserExpenditureSettings_ManualTdee", "CK_UserExpenditureSettings_Revision"])
          && emp.GetCheckConstraints().Single().Name == "CK_ExpenditureModePeriods_Static",
          "S25 schema: check constraint Manual⇔giá trị > 0, Revision ≥ 0, Static⇔giá trị > 0");
    Check(emp.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(["UserId", "EffectiveFrom"])) && !emp.FindProperty("Mode")!.IsNullable && emp.FindProperty("StaticTdeeKcal")!.IsNullable
          && emp.FindProperty("EffectiveFrom")!.ClrType == typeof(DateOnly) && emp.GetForeignKeys().Single().DeleteBehavior == Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade,
          "S25 schema: ExpenditureModePeriods UNIQUE(UserId, EffectiveFrom) ⇒ không chồng khoảng, ngày là DateOnly (local), FK cascade");
    Check(snap.FindProperty("CalculationRevision")!.IsNullable && snap.FindProperty("CalculationRevision")!.ClrType == typeof(int?) && prof.FindProperty("AthleticPursuits")!.IsNullable
          && prof.FindProperty("AthleticPursuits")!.GetMaxLength() == 30 && Enum.GetNames<AthleticPursuit>().SequenceEqual(["ExerciseRegularly", "Athlete", "ProfessionalAthlete", "NoneOfTheAbove"]),
          "S25 schema: ExpenditureSnapshots.CalculationRevision và UserProfiles.AthleticPursuits nullable (dòng cũ đọc được, NULL = Legacy/chưa trả lời); 4 đáp án đúng MF");
    Check(ues.FindProperty("BmrSex")!.IsNullable && ues.FindProperty("BmrSex")!.GetMaxLength() == 10 && prof.FindProperty("WeighedMoreThanBeforeAtWeightKg")!.IsNullable
          && prof.FindProperty("WeighedMoreThanBeforeAtWeightKg")!.GetPrecision() == 5 && prof.FindProperty("WeighedMoreThanBeforeAtWeightKg")!.GetScale() == 2 && prof.FindProperty("WeighedMoreThanBefore")!.IsNullable
          && model.FindEntityType(typeof(UserProfile))!.FindProperty("PriorMaxWeightKg") is null,
          "S25 schema: BmrSex nullable (không mặc định), WeighedMoreThanBeforeAtWeightKg decimal(5,2) nullable (cân lúc trả lời), đáp án Prior Weight vẫn định tính, KHÔNG có PriorMaxWeightKg");
    Check(ues.FindProperty("DefaultCalculationStartDate")!.IsNullable && ues.FindProperty("DefaultCalculationStartDate")!.ClrType == typeof(DateOnly?) && ues.FindProperty("CalculationStartDate")!.IsNullable
          && new UserExpenditureSettings().DefaultCalculationStartDate is null,
          "S25 schema: DefaultCalculationStartDate (ổn định, ghi một lần) tách khỏi CalculationStartDate (tuỳ chỉnh), cùng nullable DateOnly, dòng mới mặc định NULL (không bịa ngày cho legacy)");
    Check(prof.FindProperty("WeighedMoreThanBeforeThresholdKg")!.IsNullable && prof.FindProperty("WeighedMoreThanBeforeThresholdKg")!.GetPrecision() == 8 && prof.FindProperty("WeighedMoreThanBeforeThresholdKg")!.GetScale() == 4
          && prof.FindProperty("WeighedMoreThanBeforeThresholdShown")!.IsNullable && prof.FindProperty("WeighedMoreThanBeforeThresholdShown")!.GetPrecision() == 7 && prof.FindProperty("WeighedMoreThanBeforeThresholdShown")!.GetScale() == 2
          && prof.FindProperty("WeighedMoreThanBeforeThresholdUnit")!.IsNullable && prof.FindProperty("WeighedMoreThanBeforeThresholdUnit")!.GetMaxLength() == 2,
          "S25 schema: Prior Weight lưu ngưỡng chính xác decimal(8,4) + số hiển thị decimal(7,2) + đơn vị nvarchar(2), đều nullable (đáp án cũ không backfill)");
    var s25Migration = new Infrastructure.Migrations.S25_ExpenditureSettings();
    var up = s25Migration.UpOperations;
    var down = s25Migration.DownOperations;
    Check(up.All(o => o is Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation or Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation or Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation)
          && up.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>().All(o => o.IsNullable) && up.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation>().Count() == 2,
          "S25 migration Up: chỉ AddColumn(NULL)/CreateTable×2/CreateIndex — không Drop/Rename/AlterColumn/SQL thô, không backfill");
    Check(down.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.DropTableOperation>().Select(o => o.Name).Order().SequenceEqual(["ExpenditureModePeriods", "UserExpenditureSettings"])
          && down.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.DropColumnOperation>().Count() == 6, "S25 migration Down: chỉ drop 2 bảng mới + 6 cột mới (cảnh báo mất dữ liệu đã ghi trong file)");
    Check(snap.FindProperty("EstimatedTdeeKcal")!.GetPrecision() == 8 && snap.FindProperty("AlgorithmVersion")!.GetDefaultValue() as string == "1" && snap.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(["UserId", "SnapshotDate"])),
          "S25 legacy: cột/chỉ mục cũ của ExpenditureSnapshot giữ nguyên (AlgorithmVersion mặc định '1', unique UserId+SnapshotDate)");
}

// ---- Batch 1 (PCI-001/002/003/004): từ chối target không khả thi + mã lý do + gợi ý shift nhỏ hơn (vector tính tay/Python độc lập, không dùng công thức của code) ----
{
    var b1Today = new DateOnly(2026, 6, 10);
    MacroProgram B1Program(ProgramType type, int? floor = null, CalorieShiftMode mode = CalorieShiftMode.Even, decimal? pct = null, DaysOfWeek days = DaysOfWeek.None)
        => new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), ProgramType = type, GoalType = GoalType.WeightLoss, DietStyle = DietStyle.Balanced, WeeklyWeightChangeTargetPercent = 1m, ProteinGPerKgBodyweight = 1.6m,
            CalorieFloorKcal = floor, CalorieShiftMode = mode, CalorieShiftPercent = pct, CalorieShiftDays = days };
    FeasibilityResponse? B1Rejected(Action a) { try { a(); return null; } catch (BadRequestException e) { return e.Details as FeasibilityResponse; } }

    // PCI-001: kcal ≤ 0. TDEE 1000, 100 kg, 1.5 %/wk: 1.5 kg × 7700 / 7 = 1650 ⇒ 1000 − 1650 = −650; không sàn ⇒ KHÔNG kẹp, bị từ chối
    var kNeg = ProgramWeekBuilder.ComputeTargets(1000, 1.5m, 100m, 7700, null, ProgramType.Collaborative, 1.6m, null, DietStyle.Balanced);
    var kNegR = TargetFeasibility.Assess(kNeg, CalorieShiftMode.Even, null, DaysOfWeek.None, null);
    Check(kNeg.Calories == -650 && !kNegR.IsFeasible && kNegR.ReasonCodes.SequenceEqual([TargetFeasibility.InvalidCalorieTarget, TargetFeasibility.InfeasibleProgram])
          && kNegR.Violations.Single() is { Detail: TargetFeasibility.NonPositiveCalories, Calories: -650 } && kNegR.SuggestedAction == TargetFeasibility.AdjustGoalRate && kNegR.ProposedDailyTargets.Count == 0 && kNegR.Message!.Contains("greater than 0"),
          "PCI-001: kcal −650 (không sàn) bị từ chối INVALID_CALORIE_TARGET + INFEASIBLE_PROGRAM, không kẹp, gợi ý chỉnh tốc độ, message hành động được");
    // kcal = 0 đúng biên: TDEE 1100 − 1100 (1 %/wk × 100 kg × 7700/7) = 0 cũng bị từ chối
    var kZero = ProgramWeekBuilder.ComputeTargets(1100, 1m, 100m, 7700, null, ProgramType.Collaborative, 1.6m, null, DietStyle.Balanced);
    Check(kZero.Calories == 0 && TargetFeasibility.Assess(kZero, CalorieShiftMode.Even, null, DaysOfWeek.None, null).ReasonCode == TargetFeasibility.InvalidCalorieTarget, "PCI-001: kcal = 0 bị từ chối");
    // Sàn BẬT (Coached): TDEE 1500, 100 kg, 1 %/wk ⇒ thô 400 → sàn 1200 (hành vi đã duyệt, không đổi); protein 160 g (640) + fat tối thiểu 50 g (450) = 1090 ≤ 1200 ⇒ khả thi
    var kFloor = ProgramWeekBuilder.ComputeTargets(1500, 1m, 100m, 7700, 1200, ProgramType.Coached, 1.6m, null, DietStyle.Balanced);
    Check(kFloor.Calories == 1200 && TargetFeasibility.Assess(kFloor, CalorieShiftMode.Even, null, DaysOfWeek.None, null).IsFeasible, "PCI-001: sàn bật (Coached) vẫn kẹp 1200 như đã duyệt và khả thi");
    // Sàn TẮT / Collaborative: cùng đầu vào ⇒ 400 kcal; protein 160 g (640) + fat 50 g (450) = 1090 > 400 ⇒ MACRO_BUDGET_EXCEEDED, min khả thi 1090 (không ép carbs = 0, không hạ protein)
    var kNoFloor = ProgramWeekBuilder.ComputeTargets(1500, 1m, 100m, 7700, null, ProgramType.Collaborative, 1.6m, null, DietStyle.Balanced);
    var kNoFloorR = TargetFeasibility.Assess(kNoFloor, CalorieShiftMode.Even, null, DaysOfWeek.None, null);
    Check(kNoFloor.Calories == 400 && kNoFloor.ProteinG == 160m && kNoFloorR.Violations.Single() is { Detail: TargetFeasibility.MacroExceedsCalories, MinFeasibleCalories: 1090, MacroKcal: 1090 }
          && kNoFloorR.ReasonCodes.SequenceEqual([TargetFeasibility.MacroBudgetExceeded, TargetFeasibility.InfeasibleProgram]), "PCI-001/002: sàn tắt: 400 kcal không chứa protein 160 g + fat 50 g (1090 kcal) ⇒ MACRO_BUDGET_EXCEEDED, protein giữ nguyên 160");

    // PCI-002: macro vượt kcal. 1200 kcal/100 kg/2.6 g/kg Balanced: P 260 (1040) + F 50 (450) = 1490 > 1200 (+24 %); Keto 1000 kcal/120 kg/2.4 g/kg: 288·4 + 30·4 + 60·9 = 1812
    var mo1 = MacroTargetCalculator.SplitMacros(1200, 100m, 2.6m, null, DietStyle.Balanced);
    var mo2 = MacroTargetCalculator.SplitMacros(1000, 120m, 2.4m, null, DietStyle.Keto);
    var mo3 = MacroTargetCalculator.SplitMacros(2160, 80m, 2.2m, null, DietStyle.Balanced);
    var mo1R = TargetFeasibility.Assess(mo1, CalorieShiftMode.Even, null, DaysOfWeek.None, null);
    var mo2R = TargetFeasibility.Assess(mo2, CalorieShiftMode.Even, null, DaysOfWeek.None, null);
    Check(mo1.ProteinG == 260m && mo1.FatG == 50m && mo1.CarbsG == 0m && mo1R.Violations.Single().MinFeasibleCalories == 1490 && mo1R.ReasonCode == TargetFeasibility.MacroBudgetExceeded
          && mo2.ProteinG == 288m && mo2.CarbsG == 30m && mo2.FatG == 60m && mo2R.Violations.Single().MinFeasibleCalories == 1812 && mo2R.ReasonCode == TargetFeasibility.MacroBudgetExceeded,
          "PCI-002: Balanced 1200 kcal/100 kg/2.6 g/kg cần 1490; Keto 1000 kcal/120 kg/2.4 g/kg cần 1812 ⇒ MACRO_BUDGET_EXCEEDED với kcal tối thiểu");
    Check(mo3 == new MacroTargets(2160, 176m, 202m, 72m) && TargetFeasibility.Assess(mo3, CalorieShiftMode.Even, null, DaysOfWeek.None, null).IsFeasible, "PCI-002: 2160 kcal/80 kg/2.2 g/kg Balanced = P176 C202 F72 (704+808+648 = 2160) khả thi");
    // Dung sai làm tròn 2 kcal (0.85 làm tròn macro + dư): lệch 1 kcal qua, lệch 2.8 và 5 kcal từ chối; kcal 2000, P 100, C 150.5, F 111 ⇒ 400+602+999 = 2001
    Check(TargetFeasibility.Check(null, 2000, 100m, 150.5m, 111m, null).Count == 0 && TargetFeasibility.Check(null, 2000, 100m, 153m, 110m, null).Count == 0,
          "PCI-002: dung sai: lệch 1 kcal (400+602+999) và đúng 2.0 kcal (400+612+990 = 2002) vẫn qua");
    Check(TargetFeasibility.Check(null, 2000, 100m, 150.5m, 111.2m, null) is [{ Detail: TargetFeasibility.MacroExceedsCalories }] && TargetFeasibility.Check(null, 2000, 100m, 151.5m, 111m, null) is [{ Detail: TargetFeasibility.MacroExceedsCalories }],
          "PCI-002: dung sai: vượt 2.8 kcal (400+602+1000.8) và 5 kcal bị từ chối; macro không bị đổi");
    Check(TargetFeasibility.Check(null, 2000, 100m, -0.1m, 111m, null) is [{ Detail: TargetFeasibility.NegativeMacro }], "PCI-002: macro âm bị từ chối (không ép về 0)");

    // Atomic: UpsertWeek từ chối TRƯỚC khi đổi gì (không thêm tuần, không sửa EffectiveTo của tuần trước, không đổi hàng ngày)
    var atomic = B1Program(ProgramType.Collaborative);
    var atomicPrev = new MacroProgramWeek { MacroProgramId = atomic.Id, EffectiveFrom = b1Today.AddDays(-7), CaloriesTarget = 2000, ProteinTarget = 150m, CarbsTarget = 200m, FatTarget = 66.7m, RevisionNo = 1 };
    atomic.Weeks.Add(atomicPrev);
    var atomicErr = B1Rejected(() => ProgramWeekBuilder.UpsertWeek(atomic, b1Today, kNeg, AdjustmentReason.Manual));
    Check(atomicErr is { IsFeasible: false, ReasonCode: TargetFeasibility.InvalidCalorieTarget } && atomic.Weeks.Count == 1 && atomicPrev.EffectiveTo is null && atomicPrev.CaloriesTarget == 2000,
          "PCI-001: UpsertWeek từ chối 400 + FeasibilityResponse, KHÔNG thêm tuần/không sửa tuần trước (atomic: Create/UpdateGoal/EditGoal/SetManual/Check-in đều đi qua đây)");
    var okWeek = MacroTargetCalculator.SplitMacros(2100, 80m, 1.8m, null, DietStyle.Balanced);
    ProgramWeekBuilder.UpsertWeek(atomic, b1Today, okWeek, AdjustmentReason.Manual);
    Check(atomic.Weeks.Count == 2 && atomicPrev.EffectiveTo == b1Today.AddDays(-1) && atomicPrev.CaloriesTarget == 2000, "Lịch sử: target khả thi ghi tuần MỚI, tuần đã chấp nhận trước đó giữ nguyên kcal (chỉ đóng EffectiveTo như cũ)");

    // Reject/Accept-không-đề-xuất sao chép tuần đã chấp nhận: validate:false (dữ liệu cũ không hợp lệ được báo riêng, không bị chặn/sửa)
    var legacyBad = B1Program(ProgramType.Collaborative);
    legacyBad.Weeks.Add(new MacroProgramWeek { MacroProgramId = legacyBad.Id, EffectiveFrom = b1Today.AddDays(-7), CaloriesTarget = -100, ProteinTarget = 100m, CarbsTarget = 0m, FatTarget = 0m });
    ProgramWeekBuilder.UpsertWeek(legacyBad, b1Today, new MacroTargets(-100, 100m, 0m, 0m), AdjustmentReason.Manual, validate: false);
    Check(legacyBad.Weeks.Count == 2, "Reject: sao chép tuần cũ (kể cả dữ liệu cũ không hợp lệ) không bị chặn bởi kiểm mới (validate:false)");

    // Target theo ngày (Collaborative): protein KHÔNG bị hạ âm thầm. 7×2000, protein 100 g mỗi ngày, Sat khoá; tuần mới 500 ⇒ ngân sách 3500, ngày khoá 2000 ⇒ 6 ngày chia 1500 ⇒ 250 kcal < 4×100 = 400 ⇒ MACRO_BUDGET_EXCEEDED
    MacroProgram RowsProgram(bool lockSat, bool lockAll = false)
    {
        var p = B1Program(ProgramType.Collaborative);
        foreach (var d in ProgramTargetResolver.WeekOrder)
            p.DayTargets.Add(new MacroProgramDayTarget { MacroProgramId = p.Id, DayOfWeek = d, IsLocked = lockAll || (lockSat && d == DayOfWeek.Saturday), CaloriesTarget = 2000, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 88.9m });
        p.Weeks.Add(new MacroProgramWeek { MacroProgramId = p.Id, EffectiveFrom = b1Today.AddDays(-7), CaloriesTarget = 2000, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 88.9m });
        return p;
    }
    var rowsBad = RowsProgram(lockSat: true);
    var rowsErr = B1Rejected(() => ProgramWeekBuilder.UpsertWeek(rowsBad, b1Today, new MacroTargets(500, 100m, 200m, 88.9m), AdjustmentReason.ExpenditureChange));
    Check(rowsErr is { IsFeasible: false } && rowsErr.ReasonCodes.Contains(TargetFeasibility.MacroBudgetExceeded) && rowsErr.AffectedDays.Count == 6 && !rowsErr.AffectedDays.Contains("Saturday")
          && rowsBad.DayTargets.All(d => d.ProteinTarget == 100m && d.CaloriesTarget == 2000) && rowsBad.Weeks.Count == 1,
          "PCI-001/002: Collaborative: ngân sách tuần quá thấp ⇒ từ chối, protein 100 g KHÔNG bị hạ (trước đây Apply() hạ protein về kcal/4), không ghi dở");
    // Chia lại hợp lệ vẫn đúng như cũ: tuần 2100 ⇒ ngân sách 14700, Sat khoá 2000, 6 ngày còn lại 12700 ⇒ 2116.67 ⇒ 2117×5 + 2115, protein giữ 100, tổng 14700
    var rowsOk = RowsProgram(lockSat: true);
    ProgramWeekBuilder.UpsertWeek(rowsOk, b1Today, new MacroTargets(2100, 100m, 200m, 88.9m), AdjustmentReason.ExpenditureChange);
    Check(rowsOk.DayTargets.Sum(d => d.CaloriesTarget!.Value) == 14700 && rowsOk.DayTargets.Single(d => d.DayOfWeek == DayOfWeek.Saturday).CaloriesTarget == 2000 && rowsOk.DayTargets.All(d => d.ProteinTarget == 100m)
          && rowsOk.DayTargets.Where(d => d.DayOfWeek != DayOfWeek.Saturday).All(d => d.CaloriesTarget is 2117 or 2115), "Collaborative: chia lại 7 ngày hợp lệ không đổi (14700 = 7×2100, Sat 2000, protein 100)");
    // ---- D2 (PCI-005): snapshot 7 ngày theo phiên bản tuần (đọc lịch sử từ snapshot, không từ dòng ngày hiện tại) ----
    // Create/UpsertWeek ⇒ 7 snapshot (revision 1): mỗi thứ một dòng, kcal/P/C/F đã resolve, khoá, phân bổ, lý do; tuần cũ trước D2 không bị chạm
    var d2Legacy = rowsOk.Weeks.Single(w => w.EffectiveFrom == b1Today.AddDays(-7));
    var d2W1 = rowsOk.Weeks.Single(w => w.EffectiveFrom == b1Today);
    var d2Snap1 = d2W1.DayTargetSnapshots.ToList();
    Check(d2W1.DaySnapshotRevision == 1 && d2Snap1.Count == 7 && d2Snap1.Select(s => s.DayOfWeek).Distinct().Count() == 7
          && d2Snap1.All(s => s.SnapshotRevision == 1 && s.AdjustmentReason == AdjustmentReason.ExpenditureChange && s.Distribution == TargetDistribution.DayRows)
          && d2Snap1.Single(s => s.DayOfWeek == DayOfWeek.Saturday) is { CaloriesTarget: 2000, IsLocked: true, ProteinTarget: 100m, CarbsTarget: 200m }
          && d2Snap1.Sum(s => s.CaloriesTarget) == 14700 && d2Legacy.DaySnapshotRevision is null && d2Legacy.DayTargetSnapshots.Count == 0,
          "D2: UpsertWeek ghi đúng 7 snapshot (revision 1, mỗi thứ một dòng, kcal/P/C/khoá/phân bổ/lý do); tuần cũ trước D2 không bị chạm, không backfill");

    // Lịch sử đọc từ snapshot của tuần: layout/shift HIỆN TẠI đổi sau đó (check-in tuần sau, sửa tay, đổi shift) KHÔNG làm đổi target của tuần đã chụp; tuần legacy fallback + gắn LEGACY_UNVERIFIED
    var d2P = RowsProgram(lockSat: true);
    ProgramWeekBuilder.UpsertWeek(d2P, b1Today, new MacroTargets(2100, 100m, 200m, 88.9m), AdjustmentReason.ExpenditureChange);
    var d2Week = d2P.Weeks.Single(w => w.EffectiveFrom == b1Today);
    var d2Mon = b1Today.AddDays(5);   // Thứ 2 15/06
    var d2MonBefore = ProgramTargetResolver.ForDate(d2Week, d2P, d2Mon);
    foreach (var r in d2P.DayTargets) r.CaloriesTarget = 999;
    d2P.CalorieShiftMode = CalorieShiftMode.Custom; d2P.CalorieShiftPercent = 30m; d2P.CalorieShiftDays = CalorieShiftCalculator.ToFlags([DayOfWeek.Monday]);
    var d2MonAfter = ProgramTargetResolver.ForDate(d2Week, d2P, d2Mon);
    var d2LegacyWeek = new MacroProgramWeek { EffectiveFrom = b1Today.AddDays(-14), CaloriesTarget = 2000, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 88.9m };
    var d2LegacyT = ProgramTargetResolver.ForDate(d2LegacyWeek, d2P, d2Mon);
    Check(d2MonBefore.CaloriesTarget is 2117 or 2115 && d2MonAfter == d2MonBefore && d2MonAfter.TargetProvenance == "VERIFIED" && d2LegacyT.CaloriesTarget == 999 && d2LegacyT.TargetProvenance == "LEGACY_UNVERIFIED",
          "D2: đổi layout/shift hiện tại KHÔNG đổi target tuần đã chụp (SNAPSHOT); tuần cũ không có snapshot vẫn fallback như trước nhưng gắn LEGACY_UNVERIFIED, không dùng như đã kiểm chứng");

    // Nhiều lần cập nhật CÙNG NGÀY: cùng dòng tuần, snapshot revision tăng dần, bộ cũ giữ NGUYÊN (audit target trước đó, PCI-006) và không bị ghi đè
    var d2Q = RowsProgram(lockSat: true);
    ProgramWeekBuilder.UpsertWeek(d2Q, b1Today, new MacroTargets(2100, 100m, 200m, 88.9m), AdjustmentReason.ExpenditureChange);
    var d2QWeek = d2Q.Weeks.Single(w => w.EffectiveFrom == b1Today);
    var d2Rev1 = d2QWeek.DayTargetSnapshots.Select(s => (s.DayOfWeek, s.CaloriesTarget, s.ProteinTarget, s.CarbsTarget, s.FatTarget, s.AdjustmentReason, s.SnapshotRevision)).OrderBy(s => s.DayOfWeek).ToList();
    ProgramWeekBuilder.UpsertWeek(d2Q, b1Today, new MacroTargets(2050, 100m, 200m, 88.9m), AdjustmentReason.Manual);
    ProgramWeekBuilder.UpsertWeek(d2Q, b1Today, new MacroTargets(2000, 100m, 200m, 88.9m), AdjustmentReason.Manual);
    var d2Rev1After = d2QWeek.DayTargetSnapshots.Where(s => s.SnapshotRevision == 1).Select(s => (s.DayOfWeek, s.CaloriesTarget, s.ProteinTarget, s.CarbsTarget, s.FatTarget, s.AdjustmentReason, s.SnapshotRevision)).OrderBy(s => s.DayOfWeek).ToList();
    Check(d2Q.Weeks.Count(w => w.EffectiveFrom == b1Today) == 1 && d2QWeek.DaySnapshotRevision == 3 && d2QWeek.DayTargetSnapshots.Count == 21 && d2Rev1.SequenceEqual(d2Rev1After)
          && d2QWeek.DayTargetSnapshots.Where(s => s.SnapshotRevision == 2).All(s => s.AdjustmentReason == AdjustmentReason.Manual) && d2QWeek.DayTargetSnapshots.Where(s => s.SnapshotRevision == 3).Sum(s => s.CaloriesTarget) == 14000
          && ProgramTargetResolver.ForDate(d2QWeek, d2Q, b1Today.AddDays(3)).CaloriesTarget == 2000,
          "D2/PCI-006: ghi lại nhiều lần cùng ngày thêm revision 2, 3 (21 dòng), bộ revision 1 giữ nguyên (không ghi đè), đọc theo revision hiện hành");

    // Đổi goal/check-in ở tuần SAU: phiên bản tuần mới, snapshot tuần trước không đổi; target tuần trước vẫn đúng cho các ngày thuộc tuần đó
    var d2Sat1 = b1Today.AddDays(3);
    var d2SatBefore = ProgramTargetResolver.ForDate(d2QWeek, d2Q, d2Sat1);
    ProgramWeekBuilder.UpsertWeek(d2Q, b1Today.AddDays(7), new MacroTargets(1900, 100m, 200m, 88.9m), AdjustmentReason.WeightTrend);
    var d2NextWeek = d2Q.Weeks.Single(w => w.EffectiveFrom == b1Today.AddDays(7));
    Check(d2NextWeek.DaySnapshotRevision == 1 && d2NextWeek.DayTargetSnapshots.Count == 7 && ProgramTargetResolver.ForDate(d2QWeek, d2Q, d2Sat1) == d2SatBefore && d2QWeek.EffectiveTo == b1Today.AddDays(6)
          && d2NextWeek.DayTargetSnapshots.All(s => s.AdjustmentReason == AdjustmentReason.WeightTrend), "D2: check-in/đổi goal ở tuần sau ghi phiên bản tuần mới (revision 1), snapshot tuần trước không đổi");

    // Ngày nhịn ăn + dịch kcal + khoá: snapshot lưu giá trị đã resolve và phân bổ
    var d2F = RowsProgram(lockSat: true);
    var d2Fast = d2F.DayTargets.Single(d => d.DayOfWeek == DayOfWeek.Monday);
    d2Fast.IsFastingDay = true; d2Fast.CaloriesTarget = 0; d2Fast.ProteinTarget = 0; d2Fast.CarbsTarget = 0; d2Fast.FatTarget = 0;
    ProgramWeekBuilder.UpsertWeek(d2F, b1Today, new MacroTargets(2100, 100m, 200m, 88.9m), AdjustmentReason.Manual);
    var d2FMon = d2F.Weeks.Single(w => w.EffectiveFrom == b1Today).DayTargetSnapshots.Single(s => s.DayOfWeek == DayOfWeek.Monday);
    Check(d2FMon is { IsFastingDay: true, CaloriesTarget: 0, ProteinTarget: 0, CarbsTarget: 0, FatTarget: 0 } && d2F.Weeks.Single(w => w.EffectiveFrom == b1Today).DayTargetSnapshots.Sum(s => s.CaloriesTarget) == 14700,
          "D2: ngày nhịn ăn lưu 0 kcal/macro (đã resolve), tổng 7 ngày vẫn = ngân sách tuần");
    var d2S = B1Program(ProgramType.Coached, floor: 1200, mode: CalorieShiftMode.Custom, pct: 10m, days: CalorieShiftCalculator.ToFlags([DayOfWeek.Saturday]));
    d2S.Weeks.Add(new MacroProgramWeek { MacroProgramId = d2S.Id, EffectiveFrom = b1Today.AddDays(-7), CaloriesTarget = 2000, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 66.7m });
    ProgramWeekBuilder.UpsertWeek(d2S, b1Today, new MacroTargets(2000, 100m, 200m, 66.7m), AdjustmentReason.Manual);
    var d2SWeek = d2S.Weeks.Single(w => w.EffectiveFrom == b1Today);
    var d2SSatBefore = ProgramTargetResolver.ForDay(d2SWeek, d2S, DayOfWeek.Saturday);
    // Đổi shift (shift-only): ghi phiên bản tuần MỚI từ hôm sau; tuần cũ giữ shift 10 % (không retro 20 %)
    d2S.CalorieShiftPercent = 20m;
    ProgramWeekBuilder.UpsertWeek(d2S, b1Today.AddDays(1), new MacroTargets(2000, 100m, 200m, 66.7m), AdjustmentReason.Manual);
    var d2SWeek2 = d2S.Weeks.Single(w => w.EffectiveFrom == b1Today.AddDays(1));
    Check(d2SSatBefore.CaloriesTarget == 2200 && ProgramTargetResolver.ForDay(d2SWeek, d2S, DayOfWeek.Saturday).CaloriesTarget == 2200 && ProgramTargetResolver.ForDay(d2SWeek2, d2S, DayOfWeek.Saturday).CaloriesTarget == 2400
          && d2SWeek.DayTargetSnapshots.All(s => s.Distribution == TargetDistribution.Custom) && ProgramTargetResolver.ForDay(d2SWeek, d2S, DayOfWeek.Saturday).TargetProvenance == "VERIFIED",
          "D2: đổi shift 10 → 20 % không retro ngày/tuần đã chụp (Sat 2200 giữ nguyên, tuần mới 2400), phân bổ Custom được ghi");

    // Program bị thay thế / xoá dòng ngày: lịch sử của program cũ (qua ProgramHistory + Energy Balance) vẫn đọc snapshot, không đọc dòng ngày hiện tại
    var d2Old = d2P; d2Old.StartDate = b1Today.AddDays(-30); d2Old.EndedAt = b1Today.AddDays(6);
    var d2New = B1Program(ProgramType.Collaborative); d2New.StartDate = b1Today.AddDays(7);
    d2New.Weeks.Add(new MacroProgramWeek { MacroProgramId = d2New.Id, EffectiveFrom = b1Today.AddDays(7), CaloriesTarget = 1700, ProteinTarget = 100m, CarbsTarget = 150m, FatTarget = 70m });
    var d2Progs = new[] { d2Old, d2New };
    var d2Kcal = EnergyBalanceSupport.TargetFn(d2Progs, w => w.CaloriesTarget);
    var d2PastDate = b1Today.AddDays(5);   // Thứ 2 15/06 thuộc program cũ
    var d2HistBefore = d2Kcal(d2PastDate);
    d2Old.DayTargets.Clear();   // thay program/đổi style Coached xoá dòng ngày
    var d2HistAfter = EnergyBalanceSupport.TargetFn(d2Progs, w => w.CaloriesTarget)(d2PastDate);
    Check(d2HistBefore == d2MonBefore.CaloriesTarget && d2HistAfter == d2HistBefore && ProgramHistory.TargetOn(d2Progs, d2PastDate)!.TargetProvenance == "VERIFIED" && ProgramHistory.TargetOn(d2Progs, b1Today.AddDays(8))!.CaloriesTarget == 1700,
          "D2: lịch sử Food Log/Energy Balance dùng target hiệu lực của đúng ngày từ snapshot; xoá dòng ngày/thay program không đổi lịch sử");

    // ---- Provenance target theo ngày (VERIFIED | LEGACY_UNVERIFIED): thống kê so với target loại ngày chưa xác minh; Energy Balance so với Expenditure không đổi ----
    var pvMixed = RowsProgram(lockSat: true);   // tuần legacy (b1Today−7, không snapshot) + tuần có snapshot từ b1Today
    ProgramWeekBuilder.UpsertWeek(pvMixed, b1Today, new MacroTargets(2100, 100m, 200m, 88.9m), AdjustmentReason.ExpenditureChange);
    var pvLegacyOnly = RowsProgram(lockSat: true);
    var pvFrom = b1Today.AddDays(-7); var pvTo = b1Today.AddDays(3);
    var pvLogs = Enumerable.Range(0, 11).Select(i => pvFrom.AddDays(i)).ToDictionary(d => d, d => new FoodLogSupport.LoggedDay(d, 2000m, 100m, 200m, 70m, 0m, 0m));
    var pvExp = Enumerable.Range(0, 11).Select(i => pvFrom.AddDays(i)).ToDictionary(d => d, _ => 2200m);
    List<EnergyBalanceDay> PvDays(MacroProgram[] progs) => EnergyBalanceCalculator.Days(pvFrom, pvTo, pvLogs, pvExp, EnergyBalanceSupport.TargetFn(progs), d => ProgramHistory.ProvenanceOn(progs, d));
    var pvDays = PvDays([pvMixed]);
    var pvSum = EnergyBalanceCalculator.Summary(pvDays, pvFrom, pvTo);
    var pvVerifiedMean = pvDays.Where(d => d.Date >= b1Today).Average(d => d.VsTargetKcal!.Value);
    Check(pvDays.Where(d => d.Date < b1Today).All(d => d.TargetProvenance == "LEGACY_UNVERIFIED" && d.TargetKcal.HasValue && d.VsTargetKcal.HasValue) && pvDays.Where(d => d.Date >= b1Today).All(d => d.TargetProvenance == "VERIFIED"),
          "Provenance: mỗi ngày Energy Balance có targetProvenance; ngày legacy vẫn hiện target/vsTarget tham khảo");
    Check(pvSum.UnverifiedTargetDays == 7 && pvSum.VsTargetDays == 4 && pvSum.VsExpenditureDays == 11 && Math.Abs(pvSum.AvgVsTargetKcal!.Value - pvVerifiedMean) <= 1m,
          "Provenance: thống kê so với target chỉ tính 4 ngày VERIFIED, 7 ngày LEGACY_UNVERIFIED bị loại và đếm riêng (mixed range)");
    var pvNoTarget = EnergyBalanceCalculator.Summary(EnergyBalanceCalculator.Days(pvFrom, pvTo, pvLogs, pvExp, _ => null), pvFrom, pvTo);
    Check(pvSum.AvgVsExpenditureKcal == -200m && pvSum.TotalVsExpenditureKcal == pvNoTarget.TotalVsExpenditureKcal && pvSum.AvgVsExpenditureKcal == pvNoTarget.AvgVsExpenditureKcal
          && pvSum.VsExpenditureStatus == pvNoTarget.VsExpenditureStatus && pvSum.AvgExpenditureKcal == pvNoTarget.AvgExpenditureKcal,
          "Provenance: Energy Balance vs Expenditure KHÔNG phụ thuộc target (kết quả y hệt khi không có target/khi target fallback)");
    var pvLegacySum = EnergyBalanceCalculator.Summary(PvDays([pvLegacyOnly]), pvFrom, pvTo);
    Check(pvLegacySum.UnverifiedTargetDays == 11 && pvLegacySum.VsTargetDays == 0 && pvLegacySum.VsTargetStatus == "Waiting" && pvLegacySum.AvgVsTargetKcal is null && pvLegacySum.VsExpenditureDays == 11,
          "Provenance: toàn bộ lịch sử legacy ⇒ thống kê target Waiting + unverifiedTargetDays, không bịa số; Expenditure giữ nguyên");
    var pvRelT = EnergyBalanceCalculator.Relative(pvDays, pvTo, toTarget: true).Single(r => r.Days == 7);
    var pvRelE = EnergyBalanceCalculator.Relative(pvDays, pvTo, toTarget: false).Single(r => r.Days == 7);
    var pvBuckets = EnergyBalanceCalculator.Buckets(pvDays, MetricGranularity.Week, DayOfWeek.Monday);
    Check(pvRelT.UnverifiedDays == 3 && pvRelT.LoggedDays == 4 && pvRelE.UnverifiedDays == 0 && pvRelE.LoggedDays == 7 && pvBuckets.Sum(b => b.UnverifiedTargetDays) == 7,
          "Provenance: Relative to Targets / bucket loại ngày chưa xác minh (3 trong cửa sổ 7 ngày, 7 tổng bucket); Relative to Expenditure đủ 7 ngày");

    // Nutrient series (Calories/P/F/C): AverageTarget chỉ tính ngày VERIFIED; vi chất (provenance null) không bị loại
    var pvTargetFn = EnergyBalanceSupport.TargetFn([pvMixed]);
    NutrientSeriesDay[] PvNut(bool withProvenance) => NutrientDetailCalculator.Days(pvFrom, pvTo, pvLogs, _ => 2000m, false, d => (null, pvTargetFn(d), null), 2, withProvenance ? d => ProgramHistory.ProvenanceOn([pvMixed], d) : null).ToArray();
    var pvNutMacro = NutrientDetailCalculator.Summary(PvNut(true), pvFrom, pvTo, 2);
    var pvNutMicro = NutrientDetailCalculator.Summary(PvNut(false), pvFrom, pvTo, 2);
    var pvNutVerifiedAvg = Math.Round(PvNut(true).Where(d => d.Date >= b1Today).Average(d => d.Target!.Value), 2);
    Check(pvNutMacro.UnverifiedTargetDays == 7 && pvNutMacro.AverageTarget == pvNutVerifiedAvg && pvNutMicro.UnverifiedTargetDays == 0 && pvNutMicro.AverageTarget is not null
          && PvNut(true).Count(d => d.TargetProvenance == "LEGACY_UNVERIFIED") == 7 && NutrientDetailCalculator.Buckets(PvNut(true), MetricGranularity.Week, DayOfWeek.Monday, 2).Sum(b => b.UnverifiedTargetDays) == 7,
          "Provenance: nutrient series macro: averageTarget chỉ ngày VERIFIED + unverifiedTargetDays; chất không phụ thuộc program không bị đánh dấu/loại");

    // ---- HOTFIX prod "0 of —": target HÔM NAY/TƯƠNG LAI của program đang hoạt động không phụ thuộc snapshot S26; chỉ ngày QUÁ KHỨ mới LEGACY_UNVERIFIED ----
    // "preFix" = kết quả của hàm cũ (không biết hôm nay: ForDate/Days/TargetOn không có today), chính là hành vi lỗi: mọi tuần chưa có snapshot (kể cả tuần chứa hôm nay) bị LEGACY_UNVERIFIED
    var hfToday = b1Today;   // Thứ 4
    var hfP = RowsProgram(lockSat: false);   // program tạo TRƯỚC S26: tuần (today−7) không snapshot
    hfP.Weeks.Add(new MacroProgramWeek { MacroProgramId = hfP.Id, EffectiveFrom = hfToday.AddDays(-30), CaloriesTarget = 2000, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 88.9m });
    MacroProgram[] hfProgs = [hfP];
    var hfNow = ProgramHistory.TargetOn(hfProgs, hfToday, hfToday);
    var hfPreFix = ProgramHistory.TargetOn(hfProgs, hfToday);
    // 1) Active program tạo trước S26: hôm nay vẫn có target, CURRENT_ACTIVE (không VERIFIED giả); bản cũ trả LEGACY_UNVERIFIED (FE ẩn ⇒ "0 of —")
    Check(hfNow is { TargetProvenance: "CURRENT_ACTIVE", CaloriesTarget: 2000, ProteinTarget: 100m, CarbsTarget: 200m, FatTarget: 88.9m } && hfPreFix is { TargetProvenance: "LEGACY_UNVERIFIED" },
        "Hotfix 1: program tạo trước S26, hôm nay vẫn có target (CURRENT_ACTIVE, không cần snapshot); hành vi cũ = LEGACY_UNVERIFIED (nguyên nhân lỗi)");
    // 2) Tuần hiện tại có cả ngày legacy (quá khứ) và ngày hiện tại/tương lai: tuần chứa hôm nay KHÔNG toàn bộ VERIFIED/LEGACY
    var hfProv = Enumerable.Range(-3, 7).Select(i => ProgramHistory.ProvenanceOn(hfProgs, hfToday.AddDays(i), hfToday)).ToList();
    Check(hfProv.SequenceEqual(["LEGACY_UNVERIFIED", "LEGACY_UNVERIFIED", "LEGACY_UNVERIFIED", "CURRENT_ACTIVE", "PLANNED", "PLANNED", "PLANNED"])
          && Enumerable.Range(-3, 7).All(i => ProgramHistory.TargetOn(hfProgs, hfToday.AddDays(i), hfToday)!.CaloriesTarget == 2000)
          && Enumerable.Range(-3, 7).All(i => ProgramHistory.ProvenanceOn(hfProgs, hfToday.AddDays(i)) == "LEGACY_UNVERIFIED"),
        "Hotfix 2: tuần hiện tại: 3 ngày quá khứ legacy, hôm nay CURRENT_ACTIVE, 3 ngày sau PLANNED; mọi ngày vẫn có số (bản cũ: cả 7 ngày LEGACY_UNVERIFIED)");
    // 3) Tuần lịch sử không snapshot ⇒ LEGACY_UNVERIFIED (có số tham khảo); program không còn hoạt động không bao giờ CURRENT_ACTIVE
    var hfPast = ProgramHistory.TargetOn(hfProgs, hfToday.AddDays(-20), hfToday);
    var hfEnded = RowsProgram(lockSat: false); hfEnded.IsActive = false; hfEnded.EndedAt = hfToday.AddDays(20);
    Check(hfPast is { TargetProvenance: "LEGACY_UNVERIFIED", CaloriesTarget: 2000 } && ProgramHistory.TargetOn([hfEnded], hfToday, hfToday)!.TargetProvenance == "LEGACY_UNVERIFIED",
        "Hotfix 3: tuần lịch sử không snapshot ⇒ LEGACY_UNVERIFIED (không bịa VERIFIED); program không hoạt động không được gắn CURRENT_ACTIVE");
    // 4) Tuần lịch sử có snapshot hợp lệ ⇒ VERIFIED và giá trị snapshot không đổi dù dòng hiện tại bị sửa; hôm nay vẫn lấy kế hoạch hiện hành
    var hfV = RowsProgram(lockSat: false);
    ProgramWeekBuilder.UpsertWeek(hfV, hfToday.AddDays(-30), new MacroTargets(2000, 100m, 200m, 88.9m), AdjustmentReason.Manual);
    foreach (var r in hfV.DayTargets) r.CaloriesTarget = 999;
    var hfVPast = ProgramHistory.TargetOn([hfV], hfToday.AddDays(-20), hfToday);
    var hfVNow = ProgramHistory.TargetOn([hfV], hfToday, hfToday);
    Check(hfVPast is { TargetProvenance: "VERIFIED", CaloriesTarget: 2000 } && hfVNow is { TargetProvenance: "CURRENT_ACTIVE", CaloriesTarget: 999 },
        "Hotfix 4: tuần lịch sử có snapshot ⇒ VERIFIED giữ nguyên số cũ (2000) dù dòng hiện tại đổi; hôm nay = kế hoạch hiện hành (999) CURRENT_ACTIVE");
    // 5) Target đổi sau Check-in: hôm nay + tương lai theo target mới, ngày hôm qua (tuần cũ) giữ nguyên số cũ và vẫn legacy
    var hfC = B1Program(ProgramType.Collaborative);
    hfC.Weeks.Add(new MacroProgramWeek { MacroProgramId = hfC.Id, EffectiveFrom = hfToday.AddDays(-7), CaloriesTarget = 2000, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 66.7m });
    ProgramWeekBuilder.UpsertWeek(hfC, hfToday, new MacroTargets(1900, 100m, 200m, 66.7m), AdjustmentReason.ExpenditureChange);
    Check(ProgramHistory.TargetOn([hfC], hfToday, hfToday) is { TargetProvenance: "CURRENT_ACTIVE", CaloriesTarget: 1900 } && ProgramHistory.TargetOn([hfC], hfToday.AddDays(3), hfToday) is { TargetProvenance: "PLANNED", CaloriesTarget: 1900 }
          && ProgramHistory.TargetOn([hfC], hfToday.AddDays(-1), hfToday) is { TargetProvenance: "LEGACY_UNVERIFIED", CaloriesTarget: 2000 },
        "Hotfix 5: sau Check-in hôm nay/tương lai theo target mới (1900, CURRENT_ACTIVE/PLANNED), hôm qua giữ 2000 LEGACY_UNVERIFIED");
    // 6) 7 target ngày KHÁC NHAU (dòng ngày) và Calorie Shift: giá trị đúng từng thứ + provenance theo ngày lịch của tuần chứa hôm nay (Thứ 2, 3 quá khứ; Thứ 4 hiện tại; Thứ 5 → CN kế hoạch)
    var hfD = B1Program(ProgramType.Collaborative);
    foreach (var (d, i) in ProgramTargetResolver.WeekOrder.Select((d, i) => (d, i)))
        hfD.DayTargets.Add(new MacroProgramDayTarget { MacroProgramId = hfD.Id, DayOfWeek = d, CaloriesTarget = 2000 + 100 * i, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 70m });
    var hfDWeek = new MacroProgramWeek { MacroProgramId = hfD.Id, EffectiveFrom = hfToday.AddDays(-7), CaloriesTarget = 2300, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 70m };
    hfD.Weeks.Add(hfDWeek);
    var hfDays = ProgramTargetResolver.Days(hfDWeek, hfD, hfToday);
    var hfDPre = ProgramTargetResolver.Days(hfDWeek, hfD);
    var hfS = B1Program(ProgramType.Coached, floor: 1200, mode: CalorieShiftMode.Custom, pct: 10m, days: CalorieShiftCalculator.ToFlags([DayOfWeek.Saturday]));
    var hfSWeek = new MacroProgramWeek { MacroProgramId = hfS.Id, EffectiveFrom = hfToday.AddDays(-7), CaloriesTarget = 2000, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 66.7m };
    hfS.Weeks.Add(hfSWeek);
    var hfSDays = ProgramTargetResolver.Days(hfSWeek, hfS, hfToday);
    Check(hfDays.Select(x => x.Calories).SequenceEqual([2000, 2100, 2200, 2300, 2400, 2500, 2600]) && hfDays.Select(x => x.TargetProvenance).SequenceEqual(["LEGACY_UNVERIFIED", "LEGACY_UNVERIFIED", "CURRENT_ACTIVE", "PLANNED", "PLANNED", "PLANNED", "PLANNED"])
          && hfDPre.All(x => x.TargetProvenance == "LEGACY_UNVERIFIED") && hfSDays.Single(x => x.DayOfWeek == "Saturday").Calories == 2200 && hfSDays.Single(x => x.DayOfWeek == "Wednesday") is { Calories: 1967, TargetProvenance: "CURRENT_ACTIVE" },
        "Hotfix 6: 7 target ngày khác nhau + shift 10 % (Sat 2200, còn lại 1967): giá trị đúng, provenance theo ngày lịch; bản cũ cả 7 ngày LEGACY_UNVERIFIED");
    // 7) Chưa có program: không có target, không có provenance
    Check(ProgramHistory.TargetOn(Array.Empty<MacroProgram>(), hfToday, hfToday) is null && ProgramHistory.ProvenanceOn(Array.Empty<MacroProgram>(), hfToday, hfToday) is null, "Hotfix 7: user chưa có program ⇒ target null + provenance null (FE giữ '—')");
    // 8) Ngày đổi theo múi giờ user: cùng một ngày lịch D là PLANNED / CURRENT_ACTIVE / quá khứ tuỳ ngày local của user
    var hfD0 = hfToday.AddDays(2);
    Check(ProgramHistory.ProvenanceOn(hfProgs, hfD0, hfD0.AddDays(-1)) == "PLANNED" && ProgramHistory.ProvenanceOn(hfProgs, hfD0, hfD0) == "CURRENT_ACTIVE" && ProgramHistory.ProvenanceOn(hfProgs, hfD0, hfD0.AddDays(1)) == "LEGACY_UNVERIFIED",
        "Hotfix 8: phân loại theo ngày LOCAL của user: UTC+14 (today = D+1) coi D là quá khứ, UTC−12 (today = D−1) coi D là kế hoạch");
    // 9) Dashboard / Food Log / Strategy / Energy Balance / Daily Progress / Nutrient detail cùng một nguồn: cùng số cho hôm nay
    var hfTgtFn = EnergyBalanceSupport.TargetFn(hfProgs);
    var hfEbDay = EnergyBalanceCalculator.Days(hfToday, hfToday, new Dictionary<DateOnly, FoodLogSupport.LoggedDay> { [hfToday] = new(hfToday, 0m, 0m, 0m, 0m, 0m, 0m) }, new Dictionary<DateOnly, decimal>(), hfTgtFn, d => ProgramHistory.ProvenanceOn(hfProgs, d, hfToday)).Single();
    var hfStrategyToday = ProgramTargetResolver.Days(hfP.Weeks.Single(w => w.EffectiveFrom == hfToday.AddDays(-7)), hfP, hfToday).Single(x => x.DayOfWeek == "Wednesday");
    Check(hfNow!.CaloriesTarget == hfStrategyToday.Calories && hfNow.CaloriesTarget == hfEbDay.TargetKcal && hfNow.CaloriesTarget == DailyProgressTargets.On(hfProgs, hfToday).Calories && hfNow.CaloriesTarget == hfTgtFn(hfToday)
          && hfNow.TargetProvenance == hfStrategyToday.TargetProvenance && hfNow.TargetProvenance == hfEbDay.TargetProvenance && hfEbDay.TargetKcal == 2000m,
        "Hotfix 9: Food Log / Strategy / Energy Balance / Daily Progress / Nutrient target cùng một resolver: cùng 2000 + cùng CURRENT_ACTIVE cho hôm nay");
    // 10) Thống kê: ngày quá khứ legacy bị loại và đếm; hôm nay (CURRENT_ACTIVE) được tính; Expenditure không đổi; nutrient series cùng quy tắc
    var hfLogs = Enumerable.Range(-5, 6).Select(i => hfToday.AddDays(i)).ToDictionary(d => d, d => new FoodLogSupport.LoggedDay(d, 2000m, 100m, 200m, 70m, 0m, 0m));
    var hfExp = hfLogs.Keys.ToDictionary(d => d, _ => 2200m);
    var hfEb = EnergyBalanceCalculator.Days(hfToday.AddDays(-5), hfToday, hfLogs, hfExp, hfTgtFn, d => ProgramHistory.ProvenanceOn(hfProgs, d, hfToday));
    var hfSum = EnergyBalanceCalculator.Summary(hfEb, hfToday.AddDays(-5), hfToday);
    var hfNut = NutrientDetailCalculator.Days(hfToday.AddDays(-5), hfToday, hfLogs, _ => 2000m, false, d => (null, hfTgtFn(d), null), 2, d => ProgramHistory.ProvenanceOn(hfProgs, d, hfToday));
    var hfNutSum = NutrientDetailCalculator.Summary(hfNut, hfToday.AddDays(-5), hfToday, 2);
    var hfEbOld = EnergyBalanceCalculator.Summary(EnergyBalanceCalculator.Days(hfToday.AddDays(-5), hfToday, hfLogs, hfExp, hfTgtFn, d => ProgramHistory.ProvenanceOn(hfProgs, d)), hfToday.AddDays(-5), hfToday);
    Check(hfSum.UnverifiedTargetDays == 5 && hfSum.VsTargetDays == 1 && hfSum.VsExpenditureDays == 6 && hfSum.AvgVsExpenditureKcal == -200m && hfNutSum.UnverifiedTargetDays == 5 && hfNutSum.AverageTarget == 2000m
          && hfEbOld.VsTargetDays == 0 && hfEbOld.UnverifiedTargetDays == 6,
        "Hotfix 10: thống kê loại 5 ngày quá khứ legacy, giữ hôm nay (CURRENT_ACTIVE); bản cũ loại cả hôm nay (VsTargetDays 0); Expenditure không đổi");

    // Tất cả ngày khoá + ngân sách khác tuần > 3 kcal ⇒ INFEASIBLE_PROGRAM / WEEKLY_BUDGET_NOT_CONSERVED (trước đây đề xuất bị bỏ qua âm thầm)
    var allLocked = RowsProgram(lockSat: false, lockAll: true);
    var allLockedErr = B1Rejected(() => ProgramWeekBuilder.UpsertWeek(allLocked, b1Today, new MacroTargets(1900, 100m, 200m, 88.9m), AdjustmentReason.ExpenditureChange));
    Check(allLockedErr is { IsFeasible: false } && allLockedErr.Violations.Any(v => v.Detail == TargetFeasibility.WeeklyBudgetNotConserved && v.Calories == 14000 && v.MacroKcal == 13300)
          && allLockedErr.SuggestedAction == TargetFeasibility.UnlockDays && allLockedErr.ReasonCodes.Contains(TargetFeasibility.InfeasibleProgram), "PCI-004: mọi ngày khoá và ngân sách đổi ⇒ INFEASIBLE_PROGRAM + UNLOCK_DAYS (14000 vs 13300)");

    // PCI-004: shift. Base 1500, sàn 1500, 20 % × Sat+Sun: ngày còn lại 1500×(1 − 0.2×2/5) = 1380 < 1500 ⇒ vi phạm 5 ngày; không có % ≥ 5 nào hợp lệ (cần f ≤ 0) ⇒ gợi ý chia đều (hợp lệ), KHÔNG đổi shift
    var sw1500 = new MacroTargets(1500, 100m, 162.5m, 50m);   // 400 + 650 + 450 = 1500
    var sat2 = DaysOfWeek.Saturday | DaysOfWeek.Sunday;
    var sh1 = TargetFeasibility.Assess(sw1500, CalorieShiftMode.Custom, 20m, sat2, 1500);
    Check(!sh1.IsFeasible && sh1.ReasonCodes.Contains(TargetFeasibility.ShiftInfeasible) && sh1.ReasonCode == TargetFeasibility.ShiftInfeasible && !sh1.ReasonCodes.Contains(TargetFeasibility.InfeasibleProgram)
          && sh1.AffectedDays.SequenceEqual(["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]) && sh1.Violations.All(v => v.Detail == TargetFeasibility.BelowFloor && v.Calories == 1380)
          && sh1.CurrentShiftPercent == 20m && sh1.SuggestedShiftPercent is null && sh1.SuggestedAction == TargetFeasibility.UseEvenDistribution && sh1.ProposedDailyTargets.All(d => d.Calories == 1500) && sh1.ProposedDailyTargets.Count == 7
          && sh1.CurrentDailyTargets.Single(d => d.DayOfWeek == "Saturday").Calories == 1800 && sh1.Actions.SequenceEqual([TargetFeasibility.UseEvenDistribution]),
          "PCI-004: 20 % × 2 ngày, sàn 1500, base 1500 ⇒ 1380 < 1500; không shift nào hợp lệ ⇒ gợi ý chia đều tạm thời (cần user xác nhận), trả trước/sau 7 ngày");
    // Base 1650: 20 % ⇒ 1518 hợp lệ; 25 % ⇒ 1485 < 1500; % lớn nhất còn hợp lệ (kiểm lại từng bước 0.01, Python: 22.80) ⇒ gợi ý REDUCE_SHIFT; ngày chọn 2026, ngày còn lại 1500
    var sw1650 = new MacroTargets(1650, 100m, 200m, 50m);          // 400 + 800 + 450 = 1650
    Check(TargetFeasibility.Assess(sw1650, CalorieShiftMode.Custom, 20m, sat2, 1500).IsFeasible, "PCI-004: base 1650 + 20 % × 2 ngày ⇒ ngày còn lại 1518 ≥ sàn: khả thi");
    var sh2 = TargetFeasibility.Assess(sw1650, CalorieShiftMode.Custom, 25m, sat2, 1500);
    Check(!sh2.IsFeasible && sh2.SuggestedShiftPercent == 22.80m && sh2.SuggestedAction == TargetFeasibility.ReduceShift && sh2.Actions.SequenceEqual([TargetFeasibility.ReduceShift, TargetFeasibility.UseEvenDistribution])
          && sh2.Violations.All(v => v.Calories == 1485) && sh2.ProposedDailyTargets.Where(d => d.DayOfWeek is not ("Saturday" or "Sunday")).All(d => d.Calories == 1500) && sh2.ProposedDailyTargets.Where(d => d.DayOfWeek is "Saturday" or "Sunday").All(d => d.Calories == 2026)
          && Math.Abs(sh2.ProposedDailyTargets.Sum(d => d.Calories) - 7 * 1650) <= 4 && TargetFeasibility.Assess(sw1650, CalorieShiftMode.Custom, 22.81m, sat2, 1500).IsFeasible == false && TargetFeasibility.Assess(sw1650, CalorieShiftMode.Custom, 22.80m, sat2, 1500).IsFeasible,
          "PCI-004: % lớn nhất còn hợp lệ do kiểm lại (22.80, không giảm cố định): ngày còn lại 1500, ngày chọn 2026, tổng tuần 11552 (±2 làm tròn so với 11550); 22.81 vẫn vi phạm");
    // Gợi ý được áp dụng ⇒ hợp lệ (xác nhận bằng chính kiểm); Even không bao giờ vi phạm shift
    Check(TargetFeasibility.Assess(sw1650, CalorieShiftMode.Custom, sh2.SuggestedShiftPercent, sat2, 1500).IsFeasible && TargetFeasibility.Assess(sw1650, CalorieShiftMode.Even, 25m, sat2, 1500).IsFeasible, "PCI-004: áp đúng % gợi ý ⇒ hợp lệ; mode Even luôn hợp lệ");
    // Even không hợp lệ (tuần không hợp lệ) ⇒ INFEASIBLE_PROGRAM, không đề xuất shift/ even
    var sh3 = TargetFeasibility.Assess(new MacroTargets(-50, 100m, 0m, 0m), CalorieShiftMode.Custom, 10m, sat2, null);
    Check(!sh3.IsFeasible && sh3.ReasonCodes.Contains(TargetFeasibility.InfeasibleProgram) && sh3.SuggestedShiftPercent is null && sh3.ProposedDailyTargets.Count == 0 && sh3.CurrentShiftPercent == 10m, "PCI-004: tuần không hợp lệ thì chia đều cũng không cứu được ⇒ INFEASIBLE_PROGRAM, không đề xuất sai");
    // UpsertWeek áp kiểm shift sau check-in/goal/TDEE đổi: base mới 1500 trên program Coached shift 20 % ⇒ từ chối, không đổi shift
    var shiftProg = B1Program(ProgramType.Coached, floor: 1500, mode: CalorieShiftMode.Custom, pct: 20m, days: sat2);
    shiftProg.Weeks.Add(new MacroProgramWeek { MacroProgramId = shiftProg.Id, EffectiveFrom = b1Today.AddDays(-7), CaloriesTarget = 1650, ProteinTarget = 100m, CarbsTarget = 200m, FatTarget = 50m });
    var shiftErr = B1Rejected(() => ProgramWeekBuilder.UpsertWeek(shiftProg, b1Today, sw1500, AdjustmentReason.ExpenditureChange));
    Check(shiftErr is { ReasonCode: TargetFeasibility.ShiftInfeasible } && shiftProg.CalorieShiftPercent == 20m && shiftProg.CalorieShiftDays == sat2 && shiftProg.Weeks.Count == 1, "PCI-004: check-in/goal/TDEE làm shift không khả thi ⇒ từ chối, shift và tuần đã chấp nhận giữ nguyên");
    var chk = ProgramWeekBuilder.Assess(shiftProg, sw1500);
    Check(!chk.IsFeasible && chk.SuggestedAction == TargetFeasibility.UseEvenDistribution, "Check-in preview: cùng Assess ⇒ cờ INFEASIBLE + gợi ý, không tự áp dụng");

    // Check-in đề xuất không khả thi: Collaborative target 100 kcal, TDEE 1000, 100 kg, 1.5 %/wk ⇒ thô −650, hedge −150 ⇒ −50 kcal
    var ciProg = B1Program(ProgramType.Collaborative);
    ciProg.WeeklyWeightChangeTargetPercent = 1.5m;
    var ciPrev = new MacroProgramWeek { MacroProgramId = ciProg.Id, EffectiveFrom = b1Today.AddDays(-7), CaloriesTarget = 100, ProteinTarget = 20m, CarbsTarget = 5m, FatTarget = 5m };
    var ciOpt = new AlgorithmOptions();
    var ciProposal = CheckInCalculator.Propose(ciProg, 100m, 1000, ciPrev, ciOpt);
    var ciAssess = ProgramWeekBuilder.Assess(ciProg, ciProposal.Targets);
    Check(ciProposal.NewCalories == -50 && !ciAssess.IsFeasible && ciAssess.ReasonCode == TargetFeasibility.InvalidCalorieTarget, "Check-in: đề xuất −50 kcal bị gắn cờ không khả thi (Accept bị từ chối, target đã chấp nhận giữ nguyên)");
    var ciErr = B1Rejected(() => ProgramWeekBuilder.UpsertWeek(ciProg, b1Today, ciProposal.Targets, AdjustmentReason.ExpenditureChange));
    Check(ciErr is { IsFeasible: false } && ciProg.Weeks.Count == 0, "Check-in: Accept không ghi tuần khi đề xuất không khả thi");

    // Preview/Create nhất quán: cùng đầu vào không khả thi ⇒ cùng IsFeasible + mã lý do ở onboarding preview, draft preview (TryDays) và Create (Days ném 400)
    var b1Input = new ProgramPreviewInput(GoalType.WeightLoss, null, 1.5m, DietStyle.Balanced, ProteinLevel.Moderate, CalorieFloorChoice.Off, TrainingType.None, CalorieShiftMode.Even, Array.Empty<DayOfWeek>(), null, 1000, ProgramType.Collaborative);
    var b1Prev = ProgramPreviewCalculator.Compute(c7Profile, 80m, b1Input, 7700, c7Today);
    var b1Cmd = new CreateProgramCommand(GoalType.WeightLoss, 1.5m, ProgramType.Collaborative, DietStyle.Balanced, ProteinLevel.Moderate, null, null, 1000);
    var b1Prog = ProgramDraftCalculator.BuildProgram(b1Cmd, c7Profile, 80m, c7Today);
    var (_, b1Draft) = ProgramDraftCalculator.TryDays(b1Cmd, b1Prog, 80m, 1000, c7Opt);
    var b1Create = B1Rejected(() => ProgramDraftCalculator.Days(b1Cmd, b1Prog, 80m, 1000, c7Opt));
    Check(b1Prev.Feasibility is { IsFeasible: false } && b1Draft.IsFeasible == b1Prev.Feasibility.IsFeasible && b1Draft.ReasonCodes.SequenceEqual(b1Prev.Feasibility.ReasonCodes) && b1Create is { IsFeasible: false } && b1Create.ReasonCodes.SequenceEqual(b1Draft.ReasonCodes),
          "Preview == Create: onboarding preview, draft preview và Create cùng kết luận + cùng mã lý do (INVALID_CALORIE_TARGET); preview trả 200 có Feasibility, Create ném 400");
    var b1Good = ProgramPreviewCalculator.Compute(c7Profile, 80m, b1Input with { ManualTdeeKcal = 2700, WeeklyRatePercent = 0.5m }, 7700, c7Today);
    Check(b1Good.Feasibility is { IsFeasible: true, ReasonCodes.Count: 0 }, "Preview hợp lệ: Feasibility.IsFeasible = true, không mã lý do");

    // Target 7 ngày user tự nhập: kcal ≤ 0 (không nhịn ăn) bị từ chối; nhịn ăn 0 kcal hợp lệ; macro lệch kcal vẫn chỉ là cảnh báo (MacroFactor)
    var ud = new[] { new DayTargetInput(DayOfWeek.Monday, 0, 100, 100, 50), new DayTargetInput(DayOfWeek.Tuesday, 0, 0, 0, 0, IsFastingDay: true), new DayTargetInput(DayOfWeek.Wednesday, 1000, 200, 200, 100) };
    var udR = TargetFeasibility.AssessUserDays(ud);
    Check(!udR.IsFeasible && udR.AffectedDays.SequenceEqual(["Monday"]) && udR.ReasonCode == TargetFeasibility.InvalidCalorieTarget, "Manual/Collaborative nhập tay: 0 kcal không nhịn ăn bị từ chối; ngày nhịn ăn 0 kcal và macro lệch kcal (cảnh báo) vẫn qua");
    // Cảnh báo có cấu trúc (không chặn): Wednesday 1000 kcal nhưng 200 P + 200 C + 100 F = 800+800+900 = 2500 ⇒ +1500; Thursday 2000 kcal, macro 2001 (trong dung sai 2) không cảnh báo; Friday macro 1800 vs 2000 ⇒ −200 (macro ít hơn); ngày nhịn ăn không cảnh báo; input không bị đổi
    var warnIn = new[]
    {
        new DayTargetInput(DayOfWeek.Wednesday, 1000, 200, 200, 100), new DayTargetInput(DayOfWeek.Thursday, 2000, 100, 150.5m, 111m),
        new DayTargetInput(DayOfWeek.Friday, 2000, 150, 150, 66.67m), new DayTargetInput(DayOfWeek.Saturday, 0, 0, 0, 0, IsFastingDay: true),
    };
    var warnR = TargetFeasibility.AssessUserDays(warnIn);
    Check(warnR.IsFeasible && warnR.ReasonCodes.Count == 0 && warnR.Warnings!.Count == 2 && warnR.Warnings.All(w => w.Code == "MACRO_KCAL_MISMATCH" && w.ToleranceKcal == 2m)
          && warnR.Warnings[0] is { Day: "Wednesday", Calories: 1000, MacroKcal: 2500, DifferenceKcal: 1500 } && warnR.Warnings[1] is { Day: "Friday", Calories: 2000, DifferenceKcal: -200 }
          && warnR.Warnings[0].Message.Contains("You can still save") && warnIn[0] == new DayTargetInput(DayOfWeek.Wednesday, 1000, 200, 200, 100),
          "Cảnh báo macro ≠ kcal (user nhập): có cấu trúc (day, kcal, macroKcal, chênh lệch, dung sai, message), IsFeasible = true (không chặn Save), trong dung sai không cảnh báo, ngày nhịn ăn không cảnh báo, không đổi giá trị của user");
    // kcal ≤ 0 vẫn bị từ chối NHƯNG cảnh báo của ngày khác vẫn được trả; target do thuật toán tạo ra không có cảnh báo (bị từ chối); Feasible() luôn có mảng cảnh báo rỗng
    var warnBad = TargetFeasibility.AssessUserDays([new DayTargetInput(DayOfWeek.Monday, -5, 100, 100, 50), new DayTargetInput(DayOfWeek.Wednesday, 1000, 200, 200, 100)]);
    Check(!warnBad.IsFeasible && warnBad.ReasonCode == TargetFeasibility.InvalidCalorieTarget && warnBad.Warnings!.Count == 1 && TargetFeasibility.Feasible().Warnings is { Count: 0 } && kNegR.Warnings is { Count: 0 } && mo1R.Warnings is { Count: 0 },
          "Lỗi chặn vs cảnh báo: kcal âm bị từ chối (kèm cảnh báo ngày khác), target thuật toán không phát cảnh báo mà từ chối, mảng Warnings luôn có mặt");
}

// ---- TRA-001/002: cổng dữ liệu của Weekly Change / Deficit / Projection (≥ 3 ngày cân thật trong 20 ngày và ≥ 1 trong 7 ngày; vector Python/hand độc lập) ----
{
    WeightTrendInsightsResponse Ins(DateOnly today, params (int Day, decimal Kg)[] w)
        => WeightTrendSeries.Insights(WeightTrendSeries.Daily(w.Select(x => (Td(x.Day), x.Kg)), N, today), today, N, 7700);
    bool Null3(WeightTrendInsightsResponse r) => r.WeeklyChangeKg is null && r.EnergyDeficitKcalPerDay is null && r.Projected30DaysKg is null;

    // 0 lần cân; 1 lần cân cách 7 ngày (trước đây trả 0.00 kg/tuần, 0 kcal); 2 lần cân (trước đây −0.284 kg/tuần, −313 kcal): null + INSUFFICIENT_WEIGHT_DATA, không bao giờ 0
    var tg0 = WeightTrendSeries.Insights([], Td(5), N, 7700);
    var g1 = Ins(Td(7), (0, 80m));
    var g2 = Ins(Td(10), (0, 80m), (10, 79m));
    Check(Null3(tg0) && tg0.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && tg0.WeighInCount == 0 && Null3(g1) && g1.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && g1.WeighInCount == 1
          && Null3(g2) && g2.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && g2.WeighInCount == 2 && g2.CurrentWeightKg is not null && g2.MinWeighIns == 3 && g2.RateWindowDays == 20 && g2.RecencyDays == 7,
          "TRA-001/002: 0/1/2 lần cân ⇒ weekly/deficit/projection = null + INSUFFICIENT_WEIGHT_DATA (không 0 kg/tuần, không 0 kcal); trend hiện tại vẫn có");
    // 3 lần cân thật 80 / 79.5 / 79 (ngày 0, 7, 14), hôm nay ngày 14: đủ ⇒ weekly −0.25 (Python: −0.2506), deficit −276, projection = trend + 30/7 tuần
    var g3 = Ins(Td(14), (0, 80m), (7, 79.5m), (14, 79m));
    Check(g3.RateReasonCode is null && g3.WeighInCount == 3 && g3.WeighInsLast7Days == 1 && g3.DaysSinceLastWeighIn == 0 && g3.WeeklyChangeKg == -0.25m && g3.EnergyDeficitKcalPerDay == -276m && g3.Projected30DaysKg is not null,
          "TRA-001: 3 ngày cân thật trong 20 ngày + 1 trong 7 ngày ⇒ tính: −0.25 kg/tuần, −276 kcal/ngày (Python −0.2506 / −275.7)");
    // Tăng cân và giữ cân: dấu đúng; giữ cân đủ dữ liệu cho 0.00 THẬT (khác null)
    var gGain = Ins(Td(14), (0, 70m), (7, 70.5m), (14, 71m));
    var gFlat = Ins(Td(14), (0, 80m), (7, 80m), (14, 80m));
    Check(gGain.WeeklyChangeKg == 0.25m && gGain.EnergyDeficitKcalPerDay == 276m && gFlat.RateReasonCode is null && gFlat.WeeklyChangeKg == 0m && gFlat.EnergyDeficitKcalPerDay == 0m,
          "TRA-001: tăng cân +0.25 kg/tuần/+276 kcal; giữ cân đủ dữ liệu = 0.00 thật (RateReasonCode null), khác với thiếu dữ liệu (null)");
    // 3 lần cân CÙNG ngày = 1 ngày cân (trùng ngày lấy dòng cuối); ngày nội suy không bao giờ được đếm: 3 lần cân cách nhau ~10 ngày tạo 20 điểm trend nhưng đếm 3
    var gSame = Ins(Td(5), (5, 80m), (5, 79.9m), (5, 80.1m));
    var g21 = Ins(Td(20), (1, 80m), (10, 79m), (20, 78m));
    Check(gSame.WeighInCount == 1 && gSame.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && Null3(gSame) && g21.WeighInCount == 3 && g21.DataDays == 20 && g21.RateReasonCode is null,
          "TRA-001: 3 lần cân cùng ngày = 1 ngày; ngày nội suy/carry-forward không tính (20 điểm trend, 3 ngày cân)");
    // 3 lần cân dồn trong 3 ngày đầu chuỗi (chuỗi < 7 ngày): qua cổng đếm nhưng chuỗi < 7 ngày ⇒ INSUFFICIENT (không tin âm thầm)
    var gShort = Ins(Td(2), (0, 80.4m), (1, 80.2m), (2, 80m));
    Check(gShort.WeighInCount == 3 && gShort.WeighInSpanDays == 2 && gShort.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && Null3(gShort), "TRA-001: 3 lần cân trong 3 ngày đầu (chuỗi < 7 ngày) vẫn INSUFFICIENT_WEIGHT_DATA");
    // Span guard (đã duyệt): 3 lần cân dồn trong 2 ngày sau khoảng nghỉ 57 ngày (cân cũ 85 ở ngày 0) KHÔNG còn qua cổng: span = 2 < 7 ⇒ INSUFFICIENT_WEIGHT_DATA (trước đây −0.59 kg/tuần, −644 kcal)
    var gCluster = Ins(Td(59), (0, 85m), (57, 80.2m), (58, 80.1m), (59, 80m));
    Check(gCluster.WeighInCount == 3 && gCluster.WeighInSpanDays == 2 && gCluster.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && Null3(gCluster) && gCluster.MinSpanDays == 7, "Span guard: 3 lần cân trong 2 ngày (sau khoảng nghỉ dài) ⇒ INSUFFICIENT_WEIGHT_DATA, không 0");
    // Span đúng 7 + gần đây ⇒ tính; span 6 ⇒ INSUFFICIENT; span ≥ 7 nhưng stale ⇒ STALE; span tính từ lần cân thật đầu/cuối TRONG cửa sổ (lần cân ngoài cửa sổ không tính)
    var spanOk = Ins(Td(14), (7, 80m), (10, 79.8m), (14, 79.6m));        // span 7
    var span6 = Ins(Td(14), (8, 80m), (11, 79.8m), (14, 79.6m));         // span 6
    var spanStale = Ins(Td(30), (11, 80m), (16, 79.8m), (20, 79.6m));    // span 9 trong cửa sổ nhưng lần cân cuối cách 10 ngày
    var spanOut = Ins(Td(30), (0, 90m), (22, 80m), (25, 79.8m), (28, 79.6m)); // cân ngày 0 nằm NGOÀI cửa sổ [11, 30]: span trong cửa sổ = 6
    Check(spanOk.RateReasonCode is null && spanOk.WeighInSpanDays == 7 && spanOk.WeeklyChangeKg is not null && span6.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && span6.WeighInSpanDays == 6 && Null3(span6)
          && spanStale.RateReasonCode == "STALE_WEIGHT_DATA" && Null3(spanStale) && spanOut.WeighInSpanDays == 6 && spanOut.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && WeightTrendSeries.MinSpanDays == 7,
          "Span guard: span đúng 7 + gần đây ⇒ tính; span 6 ⇒ INSUFFICIENT_WEIGHT_DATA; span ≥ 7 nhưng stale ⇒ STALE_WEIGHT_DATA; lần cân ngoài cửa sổ không tính vào span");
    // Guard khoảng trống G = 14 (MACROOS POLICY, đã duyệt): khoảng trống > 14 ngày giữa hai lần cân thật liên tiếp mà lần sau nằm TRONG cửa sổ (kể cả khoảng trống bắt đầu trước cửa sổ) ⇒ INSUFFICIENT_WEIGHT_DATA
    // Ví dụ gốc A: 85.0 ngày 0 rồi 80.2/80.1/80.0 ngày 52/56/59; B: 85.0 rồi phẳng 80.0 ×3 (thực tế 0, trước đây −0.61 kg/tuần, −673 kcal); D đối chứng không có khoảng trống = 0.00
    var limA = Ins(Td(59), (0, 85m), (52, 80.2m), (56, 80.1m), (59, 80m));
    var limB = Ins(Td(59), (0, 85m), (52, 80m), (56, 80m), (59, 80m));
    var limD = Ins(Td(59), (49, 80m), (54, 80m), (59, 80m));
    Check(limA.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && Null3(limA) && limA.MaxGapDays == 52 && limA.MaxGapDaysLimit == 14 && limB.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && Null3(limB)
          && limD.RateReasonCode is null && limD.WeeklyChangeKg == 0m && limD.MaxGapDays == 5 && WeightTrendSeries.MaxGapDays == 14,
          "Guard G=14: ví dụ gốc (85 → 80 ngày 52/56/59) ⇒ INSUFFICIENT_WEIGHT_DATA, không còn −0.61 kg/tuần giả; đối chứng không có khoảng trống = 0.00 thật");
    // Khoảng trống 15/30/52/60 ngày kết thúc trong cửa sổ ⇒ INSUFFICIENT; 14 ngày ⇒ vẫn tính (biên: > 14)
    WeightTrendInsightsResponse Gap(int gap) => Ins(Td(gap + 14), (0, 85m), (gap, 80.4m), (gap + 7, 80.2m), (gap + 14, 80m));
    var gaps = new[] { 15, 30, 52, 60 }.Select(Gap).ToArray();
    var gap14 = Ins(Td(28), (0, 82m), (14, 81m), (21, 80.5m), (28, 80m));      // khoảng trống đúng 14 ngày (ngày 0 → 14) nhưng lần sau (14) nằm TRƯỚC cửa sổ [9, 28]? không: 14 ≥ 9 ⇒ trong cửa sổ
    Check(gaps.All(g => g.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && Null3(g)) && gaps.Select(g => g.MaxGapDays).SequenceEqual([15, 30, 52, 60]) && gap14.MaxGapDays == 14 && gap14.RateReasonCode is null && gap14.WeeklyChangeKg is not null,
          "Guard G=14: khoảng trống 15/30/52/60 ngày ⇒ INSUFFICIENT_WEIGHT_DATA (MaxGapDays báo đúng); đúng 14 ngày vẫn tính (ngưỡng là > 14)");
    // Khoảng trống một phần nằm ngoài cửa sổ: lần cân trước cửa sổ ở ngày 0, lần kế tiếp ngày 25 (cửa sổ [11, 30]): khoảng trống 25 ngày CẮT NGANG biên ⇒ INSUFFICIENT dù chỉ có 5 ngày của nó nằm trong cửa sổ
    var crossing = Ins(Td(30), (0, 85m), (25, 82m), (28, 81.8m), (29, 81.7m), (30, 81.6m));
    var crossing2 = Ins(Td(40), (5, 84m), (30, 82m), (34, 81.9m), (37, 81.8m), (40, 81.7m));  // khoảng trống 25 ngày (5 → 30), lần sau (30) nằm trong cửa sổ [21, 40]
    Check(crossing.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && crossing.MaxGapDays == 25 && crossing2.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && crossing2.MaxGapDays == 25 && Null3(crossing2),
          "Guard G=14: khoảng trống bắt đầu TRƯỚC cửa sổ 20 ngày nhưng kết thúc trong cửa sổ vẫn bị kiểm (ảnh hưởng EMA/regression trong cửa sổ)");
    // Phục hồi: cùng chuỗi khi khoảng trống đã rời cửa sổ (kết thúc ngày 30; hôm nay ngày 52 ⇒ cửa sổ [33, 52] không còn chứa cặp (5, 30)) ⇒ tự khôi phục, KHÔNG Waiting vô thời hạn
    var recovered = Ins(Td(52), (5, 84m), (30, 82m), (34, 81.9m), (37, 81.8m), (40, 81.7m), (44, 81.6m), (48, 81.5m), (52, 81.4m));
    var stillIn = Ins(Td(48), (5, 84m), (30, 82m), (34, 81.9m), (37, 81.8m), (40, 81.7m), (44, 81.6m), (48, 81.5m));   // cửa sổ [29, 48] còn chứa lần cân ngày 30 ⇒ còn bị chặn
    Check(recovered.RateReasonCode is null && recovered.MaxGapDays <= 4 && recovered.WeeklyChangeKg is not null && stillIn.RateReasonCode == "INSUFFICIENT_WEIGHT_DATA" && stillIn.MaxGapDays == 25,
          "Guard G=14: phục hồi tự động khi khoảng trống rời cửa sổ (hôm nay 52 ⇒ tính lại; hôm nay 48 ⇒ còn chặn)");
    // Cân mỗi 7 ngày và 2–3 lần/tuần không bị chặn bởi guard (khoảng trống ≤ 14); loss / gain / maintenance giữ dấu đúng
    var weekly = Ins(Td(21), (0, 80m), (7, 79.7m), (14, 79.4m), (21, 79.1m));
    var twoThree = Ins(Td(20), (0, 80m), (3, 79.9m), (5, 79.8m), (8, 79.7m), (10, 79.6m), (12, 79.5m), (15, 79.4m), (17, 79.3m), (19, 79.2m), (20, 79.2m));
    var gainW = Ins(Td(21), (0, 70m), (7, 70.3m), (14, 70.6m), (21, 70.9m));
    var maintW = Ins(Td(21), (0, 75m), (7, 75m), (14, 75m), (21, 75m));
    Check(weekly.RateReasonCode is null && weekly.MaxGapDays == 7 && weekly.WeeklyChangeKg < 0m && twoThree.RateReasonCode is null && twoThree.WeeklyChangeKg < 0m && gainW.RateReasonCode is null && gainW.WeeklyChangeKg > 0m && maintW.RateReasonCode is null && maintW.WeeklyChangeKg == 0m,
          "Guard G=14: cân mỗi 7 ngày, 2–3 lần/tuần không bị chặn; loss (âm), gain (dương), maintenance (0.00 thật) đúng dấu");
    // GetWeightEntries dùng cùng RateGate: khoảng trống cắt biên được thấy qua lần cân ngay trước cửa sổ
    var rgCross = WeightTrendSeries.RateGate([Td(0), Td(25), Td(28), Td(30)], Td(30)); var rgNoPre = WeightTrendSeries.RateGate([Td(25), Td(28), Td(30), Td(29)], Td(30));
    Check(rgCross.Reason == "INSUFFICIENT_WEIGHT_DATA" && rgCross.MaxGapDays == 25 && rgNoPre.MaxGapDays <= 4, "RateGate: cần lần cân ngay trước cửa sổ để thấy khoảng trống cắt biên (GetWeightEntries truy vấn thêm lần cân đó)");
    // Lần cân cuối > 7 ngày: STALE_WEIGHT_DATA (đủ 3 lần cân trong 20 ngày); đúng 6 ngày trước vẫn còn trong 7 ngày, 7 ngày trước là STALE
    var wBase = new[] { (0, 80m), (4, 79.8m), (8, 79.6m) };
    var stale8 = Ins(Td(16), wBase);          // lần cân cuối (ngày 8) cách 8 ngày
    var edge7 = Ins(Td(15), wBase);           // cách 7 ngày: ngoài [today−6, today]
    var edge6 = Ins(Td(14), wBase);           // cách 6 ngày: còn trong 7 ngày
    Check(stale8.RateReasonCode == "STALE_WEIGHT_DATA" && stale8.WeighInCount == 3 && stale8.WeighInsLast7Days == 0 && stale8.DaysSinceLastWeighIn == 8 && Null3(stale8)
          && edge7.RateReasonCode == "STALE_WEIGHT_DATA" && edge6.RateReasonCode is null && edge6.WeighInsLast7Days == 1 && edge6.WeeklyChangeKg is not null,
          "TRA-002: lần cân cuối > 7 ngày ⇒ STALE_WEIGHT_DATA (null); đúng 6 ngày trước còn tính, 7 ngày trước là stale");
    // Biên cửa sổ 20 ngày: lần cân cách hôm nay 20 ngày KHÔNG tính (cửa sổ [today−19, today])
    var tWin = WeightTrendSeries.RateGate([Td(0), Td(1), Td(19)], Td(20));
    Check(tWin.Count == 2 && tWin.Reason == "INSUFFICIENT_WEIGHT_DATA" && WeightTrendSeries.RateGate([Td(1), Td(10), Td(20)], Td(20)).Reason is null, "TRA-001: cửa sổ 20 ngày là [today−19, today]: ngày today−20 không tính");
    // Múi giờ user: lần cân 04/06; UTC 18:30 10/06 ⇒ Bangkok là 01:30 11/06 (hôm nay local 11/06: 7 ngày trước = STALE) còn UTC hôm nay là 10/06 (6 ngày trước = còn tính)
    var utcNow = new DateTimeOffset(2026, 6, 10, 18, 30, 0, TimeSpan.Zero);
    var tzDates = new[] { new DateOnly(2026, 5, 24), new DateOnly(2026, 5, 30), new DateOnly(2026, 6, 4) };
    var rBkk = WeightTrendSeries.RateGate(tzDates, UserLocale.At("Asia/Bangkok", utcNow, DayOfWeek.Monday).Today);
    var rUtc = WeightTrendSeries.RateGate(tzDates, UserLocale.At("UTC", utcNow, DayOfWeek.Monday).Today);
    Check(UserLocale.At("Asia/Bangkok", utcNow, DayOfWeek.Monday).Today == new DateOnly(2026, 6, 11) && rBkk.Reason == "STALE_WEIGHT_DATA" && rUtc.Reason is null && rUtc.InLast7Days == 1, "TRA-002: cổng dùng ngày LOCAL của user (Bangkok 11/06: lần cân 04/06 đã stale; UTC 10/06: còn tính)");
    // Expenditure state machine KHÔNG đổi: 1 lần cân/7 ngày + 4/7 log vẫn duy trì Updating (ngưỡng 3/20 chỉ cho hiển thị analytics)
    ExpenditureDay[] smDays = Enumerable.Range(0, 8).Select(i => new ExpenditureDay(Td(i), i is 1 or 3 or 4 or 6 ? 2000m : null, i == 5 ? 79.9m : null)).ToArray();
    Check(ExpenditureCalculator.MinWeighInDaysPer7 == 1 && ExpenditureCalculator.MinLoggedDaysPer7 == 4 && WeightTrendSeries.MinWeighIns == 3 && ExpenditureStatusMachine.Next(ExpenditureStatus.Updating, smDays, Td(30)) == ExpenditureStatus.Updating,
          "Expenditure/G0 không đổi: vẫn 1 lần cân/7 ngày + 4/7 log (Updating với 1 lần cân); 3/20 chỉ là điều kiện HIỂN THỊ analytics");
}

// ---- Phân loại tốc độ có xét bất định (INACTIVE): estimator khớp Python (vector độc lập) + bộ phân loại ON/FASTER/SLOWER/UNCERTAIN/WAITING ----
{
    var rb = new DateOnly(2026, 1, 1);
    // Vector 1: 30 lần cân (ngày 0..43, thưa), y = 80 − 0.07d + 0.3·sin(1.7d); Python (phân số/float độc lập): slope −0.4873891157994255, φ −0.03669701406569126, σ 0.24134643308458673, ρ 0.35298258651757486, SE 0.04465730394577146
    var d1 = new[] { 0, 1, 2, 4, 5, 7, 8, 9, 11, 12, 14, 15, 16, 18, 19, 21, 22, 23, 25, 26, 28, 29, 31, 32, 35, 36, 38, 40, 42, 43 };
    var rq1 = RateUncertainty.Estimate(d1.Select(d => (rb.AddDays(d), (decimal)(80 - 0.07 * d + 0.3 * Math.Sin(1.7 * d)))), rb.AddDays(43), 20)!;
    bool RNear(double a, double b, double tol = 1e-6) => Math.Abs(a - b) <= tol;
    Check(RNear(rq1.RateKgPerWeek, -0.4873891157994255, 2e-4) && RNear(rq1.WarmupBias, -0.03669701406569126, 2e-4) && rq1.StandardError is { } se1 && RNear(se1, 0.04465730394577146, 2e-3) && rq1.NoisePoints == 18 && rq1.Rho is { } rho1 && RNear(rho1, 0.35298258651757486, 2e-2) && rq1.Sigma is { } sg1 && RNear(sg1, 0.24134643308458673, 2e-3),
          "Estimator khớp Python độc lập (slope −0.4874, φ −0.0367, σ 0.2413, SE 0.0447; sai khác do làm tròn kg 2 chữ số của decimal ≤ 2e-4)");
    // Vector 2: chuỗi trẻ (28 ngày): φ = −0.182 (Python) ⇒ sai lệch khởi động lớn
    var d2 = new[] { 0, 2, 3, 5, 6, 8, 9, 10, 12, 13, 15, 17, 19, 20, 22, 24, 26, 27 };
    var rq2 = RateUncertainty.Estimate(d2.Select(d => (rb.AddDays(d), (decimal)(80.5 - 0.1 * d + 0.25 * Math.Cos(2.3 * d)))), rb.AddDays(27), 20)!;
    Check(RNear(rq2.WarmupBias, -0.18200437551058157, 2e-4) && RNear(rq2.RateKgPerWeek, -0.6121371221257967, 2e-3) && rq2.StandardError is { } se2 && RNear(se2, 0.04006289409493744, 2e-3), "Estimator chuỗi trẻ khớp Python (φ −0.182, slope −0.612, SE 0.0401)");
    // Tuyến tính: slope = Σ cᵢ·yᵢ chính xác; φ theo tuổi chuỗi (Python): 21 ngày −0.3318, 35 −0.0817, 56 −0.0100 (cân mỗi ngày)
    var ddaily = Enumerable.Range(0, 36).ToArray();
    var (sl, cw) = RateUncertainty.SlopeAndWeights(d1, d1.Select(d => 80 - 0.07 * d + 0.3 * Math.Sin(1.7 * d)).ToArray(), 43, 2.0 / 21);
    double Phi(int age) => RateUncertainty.SlopeAndWeights(Enumerable.Range(0, age + 1).ToArray(), Enumerable.Range(0, age + 1).Select(d => 80 + d / 7.0).ToArray(), age, 2.0 / 21).Slope - 1;
    Check(RNear(sl, cw.Zip(d1, (c, d) => c * (80 - 0.07 * d + 0.3 * Math.Sin(1.7 * d))).Sum(), 1e-9) && RNear(Phi(21), -0.3318, 5e-4) && RNear(Phi(35), -0.0817, 5e-4) && RNear(Phi(56), -0.0100, 5e-4) && ddaily.Length == 36,
          "Estimator: slope = Σ cᵢ·yᵢ chính xác (tuyến tính); sai lệch khởi động φ(21)=−0.332, φ(35)=−0.082, φ(56)=−0.010 khớp Python");
    // < 3 lần cân ⇒ null; < 6 lần cân trong 28 ngày ⇒ có slope nhưng KHÔNG có SE
    Check(RateUncertainty.Estimate([(rb, 80m), (rb.AddDays(5), 79.9m)], rb.AddDays(6), 20) is null && RateUncertainty.Estimate(new[] { 0, 3, 7, 10, 14 }.Select(d => (rb.AddDays(d), 80m - d * 0.02m)), rb.AddDays(14), 20) is { StandardError: null } , "Estimator: <3 lần cân ⇒ null; 5 lần cân ⇒ có tốc độ nhưng không có SE");

    // Bộ phân loại (vector tay; δ = 0.10, k = 1.645, λ = 1.3 ⇒ k·λ = 2.1385; h = 2.1385·SE + |φ||r̂|/(1−|φ|))
    var op = new AlgorithmOptions();
    RateClassification Cl(GoalDirection dir, double target, double rate, double? se, double phi = 0, string? gate = null)
        => RateClassifier.Classify(dir, target, gate, new RateUncertainty.RateEstimate(rate, se, phi, se is null ? null : 0.5, se is null ? null : 0.4, 20), op);
    // Loss mục tiêu −0.5: thực tế −0.52, SE 0.02 ⇒ dev 0.02, h 0.0428 ⇒ [−0.023, 0.063] trong ±0.10 ⇒ ON_TARGET
    // thực tế −0.52, SE 0.05 ⇒ h 0.1069 ⇒ [−0.087, 0.127]: điểm ước lượng trong δ nhưng khoảng KHÔNG nằm hẳn trong ±δ ⇒ UNCERTAIN (không phải ON_TARGET)
    Check(Cl(GoalDirection.Loss, -0.5, -0.52, 0.02).State == "ON_TARGET" && Cl(GoalDirection.Loss, -0.5, -0.52, 0.05).State == "UNCERTAIN" && Cl(GoalDirection.Loss, -0.5, -0.52, 0.05).Deviation is { } dv && RNear(dv, 0.02),
          "ON_TARGET chỉ khi TOÀN BỘ khoảng bất định nằm trong ±δ: SE 0.02 ⇒ ON_TARGET; cùng điểm ước lượng với SE 0.05 ⇒ UNCERTAIN");
    // FASTER/SLOWER cần bằng chứng ngoài ±δ theo hướng goal: Loss −0.5: −0.9 (dev +0.4, h 0.107 ⇒ dưới 0.293 > 0.1) FASTER; −0.1 (dev −0.4 ⇒ trên −0.293 < −0.1) SLOWER; −0.65 (dev +0.15, h 0.107 ⇒ dưới 0.043 < 0.1) UNCERTAIN
    Check(Cl(GoalDirection.Loss, -0.5, -0.9, 0.05).State == "FASTER_THAN_TARGET" && Cl(GoalDirection.Loss, -0.5, -0.1, 0.05).State == "SLOWER_THAN_TARGET" && Cl(GoalDirection.Loss, -0.5, -0.65, 0.05).State == "UNCERTAIN",
          "Loss: giảm nhanh hơn mục tiêu = FASTER, chậm hơn = SLOWER, chỉ khi cả khoảng nằm ngoài ±δ; vùng giữa = UNCERTAIN");
    // Gain / MuscleGain (hướng Gain) mục tiêu +0.25: thực tế +0.60 (dev +0.35) FASTER; 0.00 (dev −0.25, h 0.107 ⇒ trên −0.143 < −0.1) SLOWER; +0.30 (dev 0.05, h .107) UNCERTAIN
    Check(Cl(GoalRules.DirectionOf(GoalType.WeightGain), 0.25, 0.60, 0.05).State == "FASTER_THAN_TARGET" && Cl(GoalRules.DirectionOf(GoalType.MuscleGain), 0.25, 0.0, 0.05).State == "SLOWER_THAN_TARGET" && Cl(GoalRules.DirectionOf(GoalType.MuscleGain), 0.25, 0.30, 0.05).State == "UNCERTAIN"
          && Cl(GoalDirection.Gain, 0.25, 0.26, 0.02).State == "ON_TARGET", "Gain/MuscleGain: tăng nhiều hơn mục tiêu = FASTER (không bao giờ coi như giảm cân), ít hơn = SLOWER");
    // Maintenance (mục tiêu 0): 0.02/SE 0.02 ON_TARGET; −0.5 LOSING_WEIGHT; +0.5 GAINING_WEIGHT; 0.15/SE 0.05 UNCERTAIN; không bao giờ FASTER/SLOWER
    var mStates = new[] { Cl(GoalDirection.Maintain, 0, 0.02, 0.02), Cl(GoalDirection.Maintain, 0, -0.5, 0.05), Cl(GoalDirection.Maintain, 0, 0.5, 0.05), Cl(GoalDirection.Maintain, 0, 0.15, 0.05), Cl(GoalDirection.Maintain, 0, -0.15, 0.05) }.Select(x => x.State).ToArray();
    Check(mStates.SequenceEqual(["ON_TARGET", "LOSING_WEIGHT", "GAINING_WEIGHT", "UNCERTAIN", "UNCERTAIN"]) && !mStates.Any(s => s is "FASTER_THAN_TARGET" or "SLOWER_THAN_TARGET"), "Maintenance (mục tiêu 0): ON_TARGET / LOSING_WEIGHT / GAINING_WEIGHT / UNCERTAIN, không có FASTER/SLOWER");
    // WAITING khi chưa qua cổng TRA (kèm mã lý do); không có SE ⇒ UNCERTAIN (NOISE_NOT_ESTIMABLE); chuỗi còn khởi động (|φ| > 0.15) ⇒ UNCERTAIN (WARMUP_BIAS) dù sai lệch đã rõ
    var wIns = Cl(GoalDirection.Loss, -0.5, -0.9, 0.05, 0, "INSUFFICIENT_WEIGHT_DATA"); var wStale = Cl(GoalDirection.Loss, -0.5, -0.9, 0.05, 0, "STALE_WEIGHT_DATA");
    Check(wIns is { State: "WAITING", Reason: "INSUFFICIENT_WEIGHT_DATA", Deviation: null } && wStale.State == "WAITING" && wStale.Reason == "STALE_WEIGHT_DATA" && Cl(GoalDirection.Loss, -0.5, -0.9, null) is { State: "UNCERTAIN", Reason: "NOISE_NOT_ESTIMABLE" }
          && Cl(GoalDirection.Loss, -0.5, -0.9, 0.05, -0.2) is { State: "UNCERTAIN", Reason: "WARMUP_BIAS" } && RateClassifier.Classify(GoalDirection.Loss, -0.5, null, null, op).State == "WAITING",
          "WAITING = chưa qua cổng TRA (kèm mã), không SE ⇒ UNCERTAIN, chuỗi còn khởi động ⇒ UNCERTAIN");
    // Sai lệch khởi động mở rộng khoảng: φ = −0.10, thực tế −0.9, SE 0.05 ⇒ bias = 0.1·0.9/0.9 = 0.10, h = 0.1069 + 0.10 = 0.2069; dev 0.4 ⇒ dưới 0.193 > 0.1 ⇒ vẫn FASTER; thực tế −0.75 (bias 0.083, h 0.190, dev 0.25, dưới 0.060 < 0.1) ⇒ UNCERTAIN
    Check(RNear(RateClassifier.BiasBound(-0.9, -0.10), 0.10) && Cl(GoalDirection.Loss, -0.5, -0.9, 0.05, -0.10).State == "FASTER_THAN_TARGET" && Cl(GoalDirection.Loss, -0.5, -0.75, 0.05, -0.10).State == "UNCERTAIN" && Cl(GoalDirection.Loss, -0.5, -0.75, 0.05, 0).State == "FASTER_THAN_TARGET",
          "Sai lệch khởi động φ mở rộng khoảng bất định (−0.75 với φ −0.10: bias 0.083, h 0.190, dev 0.25 ⇒ UNCERTAIN; φ 0: h 0.107 ⇒ FASTER)");
    // Biên đúng: |dev| + h = δ ⇒ ON_TARGET; tham số từ cấu hình trung tâm (δ, k, λ), không hằng số cứng
    var optWide = new AlgorithmOptions { RateOnTargetDeltaKgPerWeek = 0.30m };
    Check(RateClassifier.Classify(GoalDirection.Loss, -0.5, null, new RateUncertainty.RateEstimate(-0.62, 0.05, 0, 0.5, 0.4, 20), optWide).State == "ON_TARGET" && Cl(GoalDirection.Loss, -0.5, -0.62, 0.05).State == "UNCERTAIN"
          && op.RateOnTargetDeltaKgPerWeek == 0.10m && op.RateOnTargetConfidenceK == 1.645m && op.RateSeInflation == 1.3m && op.RateMaxWarmupBias == 0.15m && !op.RateClassificationEnabled,
          "Tham số δ/k/λ/φ-cap lấy từ AlgorithmOptions (mặc định 0.10/1.645/1.3/0.15), công tắc RateClassificationEnabled = false (INACTIVE)");
    // Đơn điệu: SE lớn hơn không bao giờ biến UNCERTAIN thành quyết định; SE lớn đến mấy cũng không ON_TARGET/FASTER/SLOWER giả
    var monotonic = true;
    foreach (var r0 in new[] { -1.2, -0.9, -0.6, -0.5, -0.4, -0.2, 0.0 })
    {
        var prevDecisive = true;
        foreach (var se in new[] { 0.005, 0.02, 0.05, 0.1, 0.3, 1.0 })
        {
            var st = Cl(GoalDirection.Loss, -0.5, r0, se).State; var decisive = st != "UNCERTAIN";
            if (decisive && !prevDecisive) monotonic = false; prevDecisive = decisive;
        }
    }
    Check(monotonic && Cl(GoalDirection.Loss, -0.5, -0.5, 1.0).State == "UNCERTAIN", "Bất biến: SE tăng không bao giờ biến UNCERTAIN thành quyết định; SE rất lớn ⇒ UNCERTAIN");
}

// ---- FE blockers B1/B2: shift đang lưu có trong ProgramResponse; shift-only confirm hợp lệ; chỉ một định dạng infeasible ----
{
    Mapster.TypeAdapterConfig.GlobalSettings.Scan(typeof(ProgramMappingConfig).Assembly);
    var shProg = new MacroProgram { Id = Guid.NewGuid(), ProgramType = ProgramType.Coached, GoalType = GoalType.WeightLoss, DietStyle = DietStyle.Balanced, CalorieShiftMode = CalorieShiftMode.Custom, CalorieShiftPercent = 22.8m, CalorieShiftDays = DaysOfWeek.Saturday | DaysOfWeek.Sunday };
    var shResp = Mapster.TypeAdapter.Adapt<ProgramResponse>(shProg);
    var evenResp = Mapster.TypeAdapter.Adapt<ProgramResponse>(new MacroProgram { Id = Guid.NewGuid(), ProgramType = ProgramType.Coached, GoalType = GoalType.WeightLoss, DietStyle = DietStyle.Balanced });
    Check(shResp.CalorieShiftMode == "Custom" && shResp.CalorieShiftPercent == 22.8m && shResp.CalorieShiftDays!.SequenceEqual(["Saturday", "Sunday"]) && evenResp.CalorieShiftMode == "Even" && evenResp.CalorieShiftPercent is null
          && CalorieShiftCalculator.DayNames(DaysOfWeek.None).Count == 0 && CalorieShiftCalculator.DayNames(CalorieShiftCalculator.ToFlags([DayOfWeek.Sunday, DayOfWeek.Monday])).SequenceEqual(["Monday", "Sunday"]),
          "B1: GET programs/current trả calorieShiftMode/Days/Percent đang lưu (Custom, 22.8, Saturday+Sunday; Even mặc định) để FE gửi lại đúng khi xác nhận shift nhỏ hơn");
    var shV = new UpdateProgramGoalCommandValidator();
    var shOnly = new UpdateProgramGoalCommand(null, null, null, null, null, null, null, CalorieShiftMode.Custom, [DayOfWeek.Saturday, DayOfWeek.Sunday], 22.8m);
    Check(shV.Validate(shOnly).IsValid && shV.Validate(new UpdateProgramGoalCommand(null, null, null, null, null, null, null, CalorieShiftMode.Even)).IsValid
          && !shV.Validate(shOnly with { CalorieShiftPercent = null }).IsValid && !shV.Validate(shOnly with { CalorieShiftDays = null }).IsValid && !shV.Validate(shOnly with { CalorieShiftPercent = 31m }).IsValid,
          "B1: UpdateProgramGoal xác nhận shift-only hợp lệ ({Custom, ngày, %}); Even không cần ngày/%; thiếu % hoặc ngày, % ngoài 5..30 bị từ chối (không shift giả)");
    // B2: chỉ còn MỘT định dạng infeasible (FeasibilityResponse); mọi đường phát đều qua TargetFeasibility.EnsureFeasible
    Check(typeof(CalorieShiftFeasibility).GetMethod("EnsureFeasible") is null && typeof(FeasibilityResponse).Assembly.GetType("Contract.CalorieShiftInfeasibleModel") is null, "B2: định dạng cũ CalorieShiftInfeasibleModel/EnsureFeasible đã bỏ; FeasibilityResponse là định dạng duy nhất");
}

// ---- TRA-003 groundwork (INACTIVE, chưa nối): ActiveExpenditureResolver = Static ? giá trị Static : TDEE đã học; lớp phủ theo ngày, Static → Dynamic giữ nguyên lịch sử học ----
{
    var sb0 = new DateOnly(2026, 3, 1); var uid = Guid.NewGuid();
    List<ExpenditureModePeriod> none = [];
    var p1 = ExpenditureModeTimeline.Apply(none, uid, ExpenditureMode.Static, 2500, sb0.AddDays(10));
    var p2 = ExpenditureModeTimeline.Apply(p1, uid, ExpenditureMode.Dynamic, null, sb0.AddDays(20));
    Check(ActiveExpenditureResolver.Resolve(none, sb0.AddDays(5), 2310m) == (2310m, "Learned")
          && ActiveExpenditureResolver.Resolve(p1, sb0.AddDays(9), 2310m) == (2310m, "Learned") && ActiveExpenditureResolver.Resolve(p1, sb0.AddDays(10), 2310m) == (2500m, "Static") && ActiveExpenditureResolver.Resolve(p1, sb0.AddDays(15), 2400m) == (2500m, "Static")
          && ActiveExpenditureResolver.Resolve(p2, sb0.AddDays(19), 2400m) == (2500m, "Static") && ActiveExpenditureResolver.Resolve(p2, sb0.AddDays(20), 2380m) == (2380m, "Learned") && ActiveExpenditureResolver.Resolve(p2, sb0.AddDays(30), 2390m) == (2390m, "Learned")
          && ActiveExpenditureResolver.IsStatic(p2, sb0.AddDays(12)) && !ActiveExpenditureResolver.IsStatic(p2, sb0.AddDays(25)) && !ActiveExpenditureResolver.IsStatic(none, sb0),
          "TRA-003 (chưa nối): Static từ ngày 10 ⇒ 2500 (không phụ thuộc TDEE đã học); trước ngày 10 và sau khi về Dynamic ngày 20 ⇒ dùng TDEE đã học; lịch sử Static/Dynamic theo ngày LOCAL");
}

// ---- PCI-003: phân loại tốc độ theo HƯỚNG goal (chỉ đổi nhãn; ON_TARGET vẫn bằng chính xác, dung sai CHƯA duyệt) ----
{
    var dirPr = new CheckInProposal(2000, 2000, 2000, 0, false, false, 0.4m, new MacroTargets(2000, 150m, 200m, 70m));
    string C(GoalDirection d, decimal actual, decimal target) => CheckInCalculator.Classify(d, actual, target);
    // Loss: mục tiêu giảm 0.40 kg/tuần (+0.40); giảm 0.60 ⇒ FASTER; 0.20 ⇒ SLOWER; 0.40 ⇒ ON_TARGET
    Check(C(GoalDirection.Loss, 0.60m, 0.40m) == "FASTER_THAN_TARGET" && C(GoalDirection.Loss, 0.20m, 0.40m) == "SLOWER_THAN_TARGET" && C(GoalDirection.Loss, 0.40m, 0.40m) == "ON_TARGET", "PCI-003 Loss: giảm nhiều hơn mục tiêu = FASTER, ít hơn = SLOWER");
    // Gain/MuscleGain: mục tiêu tăng 0.24 (−0.24); tăng 0.45 (−0.45) ⇒ FASTER; tăng 0.05 (−0.05) ⇒ SLOWER; bằng ⇒ ON_TARGET; code cũ trả ngược lại
    Check(C(GoalDirection.Gain, -0.45m, -0.24m) == "FASTER_THAN_TARGET" && C(GoalDirection.Gain, -0.30m, -0.24m) == "FASTER_THAN_TARGET" && C(GoalDirection.Gain, -0.05m, -0.24m) == "SLOWER_THAN_TARGET" && C(GoalDirection.Gain, -0.24m, -0.24m) == "ON_TARGET"
          && GoalRules.DirectionOf(GoalType.MuscleGain) == GoalDirection.Gain && GoalRules.DirectionOf(GoalType.WeightGain) == GoalDirection.Gain, "PCI-003 Gain/MuscleGain: tăng nhiều hơn mục tiêu = FASTER, ít hơn = SLOWER (MuscleGain không bao giờ coi như giảm cân)");
    // Maintenance: không có FASTER/SLOWER theo hướng giảm cân
    Check(C(GoalDirection.Maintain, 0.20m, 0m) == "LOSING_WEIGHT" && C(GoalDirection.Maintain, -0.20m, 0m) == "GAINING_WEIGHT" && C(GoalDirection.Maintain, 0m, 0m) == "ON_TARGET"
          && new[] { 0.5m, -0.5m, 0m, 0.01m }.All(a => C(GoalDirection.Maintain, a, 0m) is not ("FASTER_THAN_TARGET" or "SLOWER_THAN_TARGET")), "PCI-003 Maintenance: chỉ LOSING_WEIGHT / GAINING_WEIGHT / ON_TARGET");
    // Mã lý do + mô tả theo goal; phân loại KHÔNG đổi kcal đề xuất (Propose không đọc nhãn)
    // Bộ phân loại (đã sửa dấu) chỉ còn chạy khi bật emitRateVerdict (mặc định TẮT, xem A1)
    var gainCodes = CheckInCalculator.ReasonCodes(dirPr, ExpenditureStatus.Updating, -0.45m, -0.24m, 0, GoalDirection.Gain, emitRateVerdict: true);
    var gainText = CheckInCalculator.Reason(dirPr, ExpenditureStatus.Updating, -0.45m, -0.24m, GoalDirection.Gain, emitRateVerdict: true);
    var maintText = CheckInCalculator.Reason(dirPr, ExpenditureStatus.Updating, 0.20m, 0m, GoalDirection.Maintain, emitRateVerdict: true);
    Check(gainCodes.SequenceEqual(["FASTER_THAN_TARGET"]) && gainText.StartsWith("Tăng nhanh hơn") && !gainText.Contains("Giảm") && maintText.Contains("giữ cân") && !maintText.Contains("nhanh hơn")
          && CheckInCalculator.ReasonCodes(dirPr, ExpenditureStatus.Updating, 0.8m, 0.5m, 0, GoalDirection.Loss, emitRateVerdict: true).SequenceEqual(["FASTER_THAN_TARGET"]), "PCI-003: mô tả/mã theo GoalType (bộ phân loại sau cờ emitRateVerdict, mặc định Loss giữ nguyên hành vi cũ)");
    var gainProg = new MacroProgram { Id = Guid.NewGuid(), ProgramType = ProgramType.Coached, GoalType = GoalType.MuscleGain, DietStyle = DietStyle.Balanced, WeeklyWeightChangeTargetPercent = -0.3m, ProteinGPerKgBodyweight = 1.6m };
    var propLoss = CheckInCalculator.Propose(gainProg, 80m, 2600, new MacroProgramWeek { CaloriesTarget = 2300, ProteinTarget = 130m, CarbsTarget = 250m, FatTarget = 70m }, new AlgorithmOptions());
    var propLoss2 = CheckInCalculator.Propose(gainProg, 80m, 2600, new MacroProgramWeek { CaloriesTarget = 2300, ProteinTarget = 130m, CarbsTarget = 250m, FatTarget = 70m }, new AlgorithmOptions());
    Check(propLoss.NewCalories == propLoss2.NewCalories, "PCI-003: Propose không phụ thuộc nhãn phân loại (chỉ hiển thị)");
}

// ---- NUT-005/006: sửa entry giữ snapshot gốc (không đọc lại FoodItem/Recipe; không trôi số khi sửa nhiều lần; null ≠ 0) ----
{
    var nutSrv = Guid.NewGuid();
    // Entry món log theo khẩu phần: 2 × 30 g = 60 g gốc; macro gốc 120 kcal / P 6 / C 18 / F 4.5, fiber 1.2, sodium null (chưa biết)
    FoodLogEntry NutServing(bool stamped)
    {
        var e = new FoodLogEntry { Id = Guid.NewGuid(), FoodItemId = Guid.NewGuid(), FoodServingId = nutSrv, Quantity = 2m, Calories = 120m, Protein = 6m, Carbs = 18m, Fat = 4.5m, Fiber = 1.2m, Sodium = null };
        if (stamped) EntryServingSnapshot.Stamp(e, nutSrv, "1 muỗng", 30m, 60m);
        return e;
    }
    // Entry log theo gram: 150 g
    FoodLogEntry NutGram()
    {
        var e = new FoodLogEntry { Id = Guid.NewGuid(), FoodItemId = Guid.NewGuid(), Quantity = 150m, Calories = 247.5m, Protein = 22.5m, Carbs = 30.75m, Fat = 5.25m, Fiber = 3.5m, Sodium = 0m };
        EntryServingSnapshot.Stamp(e, null, null, null, 150m);
        return e;
    }

    // Đổi bữa/giờ (số lượng + khẩu phần không đổi): dinh dưỡng giữ nguyên, không có macro mới, snapshot nguyên văn
    var nutE0 = NutServing(true);
    var nutP0 = EntryEdit.Plan(nutE0, 2m, nutSrv, null);
    Check(nutP0.Unchanged && nutP0.Macros is null && nutP0.SnapshotJson == nutE0.ServingSnapshotJson, "NUT-005: đổi bữa/giờ không tính lại dinh dưỡng, giữ snapshot gốc");

    // Đổi số lượng (gram): co giãn từ macro GỐC theo tỉ lệ gram; fiber 3.5 → 4.67; sodium 0 vẫn 0 (đã biết bằng 0)
    var nutP1 = EntryEdit.Plan(NutGram(), 200m, null, null);
    Check(nutP1.Macros is { Calories: 330.00m, Protein: 30.00m, Carbs: 41.00m, Fat: 7.00m, Fiber: 4.67m, Sodium: 0m } && nutP1.Ratio > 1.3333m && nutP1.Ratio < 1.3334m, "NUT-005: đổi gram 150→200 co giãn từ snapshot gốc theo tỉ lệ 4/3 (kcal 330, fiber 4.67, sodium 0 giữ 0)");

    // Sodium chưa biết (null) vẫn null sau khi sửa: không biến thành 0
    var nutP2 = EntryEdit.Plan(NutServing(true), 3m, nutSrv, null);
    Check(nutP2.Macros is { Sodium: null, Fiber: 1.80m }, "NUT-005: chất chưa biết (null) vẫn null sau khi sửa số lượng, chất đã biết co giãn đúng");

    // Food Database đổi SAU khi log (gram khẩu phần custom 30 → 45 g, macro/100 g đổi): Plan KHÔNG nhận FoodItem/FoodServing nào ngoài lựa chọn mới ⇒ sửa số lượng chỉ dùng snapshot LÚC LOG (30 g), không bao giờ 45 g hiện tại
    var nutP3 = EntryEdit.Plan(NutServing(true), 3m, nutSrv, null);
    Check(nutP3.Macros is { Calories: 180.00m, Protein: 9.00m, Carbs: 27.00m, Fat: 6.75m } && nutP3.Ratio == 1.5m, "NUT-006: gram khẩu phần đổi sau khi log không ảnh hưởng entry (2→3 khẩu phần = ×1.5 theo 30 g lúc log, không ×2.25)");

    // Đổi sang khẩu phần khác (100 g, lựa chọn mới): gram cũ lấy từ snapshot gốc, snapshot cập nhật khẩu phần hiện tại nhưng GIỮ macro gốc
    var nutNew = Guid.NewGuid();
    var nutP4 = EntryEdit.Plan(NutServing(true), 1m, nutNew, new ServingRef(nutNew, "1 chén", 100m));
    var nutS4 = EntryServingSnapshot.Parse(nutP4.SnapshotJson)!;
    Check(nutP4.Macros is { Calories: 200.00m, Protein: 10.00m, Carbs: 30.00m, Fat: 7.50m } && nutS4.ServingId == nutNew && nutS4.ServingGrams == 100m && nutS4.BaseAmount == 60m && nutS4.Kcal == 120m,
        "NUT-006: đổi khẩu phần dùng gram khẩu phần lúc log (60 g) và gram khẩu phần mới (100 g); snapshot gốc giữ macro gốc");

    // Entry CŨ (không có snapshot gốc): cùng khẩu phần đổi số lượng không cần gram khẩu phần (tỉ lệ số lượng = tỉ lệ gram); đổi khẩu phần ⇒ TỪ CHỐI 400 LEGACY_SERVING_SNAPSHOT_MISSING (không dùng gram hiện tại, không ghi gì)
    var nutLegacyQty = EntryEdit.Plan(NutServing(false), 3m, nutSrv, null);
    Check(nutLegacyQty.Macros is { Calories: 180.00m } && nutLegacyQty.SnapshotJson is null, "NUT-005 legacy: entry không có snapshot gốc đổi số lượng co giãn từ macro đang lưu, không bịa snapshot");
    BadRequestException? nutRefusal = null;
    var nutLegacyEntry = NutServing(false);
    var nutLegacyBefore = (nutLegacyEntry.Quantity, nutLegacyEntry.FoodServingId, nutLegacyEntry.Calories, nutLegacyEntry.Protein, nutLegacyEntry.Carbs, nutLegacyEntry.Fat, nutLegacyEntry.Fiber, nutLegacyEntry.Sodium, nutLegacyEntry.ServingSnapshotJson);
    try { EntryEdit.Plan(nutLegacyEntry, 1m, nutNew, new ServingRef(nutNew, "1 chén", 100m)); } catch (BadRequestException e) { nutRefusal = e; }
    var nutRefusalData = nutRefusal?.Details as EntryEditRefusalResponse;
    Check(nutRefusal is { StatusCode: 400 } && nutRefusalData is { ReasonCode: "LEGACY_SERVING_SNAPSHOT_MISSING" } && nutRefusalData.EntryId == nutLegacyEntry.Id
          && nutRefusal.Errors!["foodServingId"].SequenceEqual(["LEGACY_SERVING_SNAPSHOT_MISSING"]) && nutRefusal.Message.Contains("không xác minh được khối lượng gốc"),
        "NUT-006 legacy: đổi khẩu phần của entry cũ ⇒ 400 + errors.foodServingId + data.reasonCode = LEGACY_SERVING_SNAPSHOT_MISSING, không dùng gram hiện tại");
    Check(nutLegacyBefore == (nutLegacyEntry.Quantity, nutLegacyEntry.FoodServingId, nutLegacyEntry.Calories, nutLegacyEntry.Protein, nutLegacyEntry.Carbs, nutLegacyEntry.Fat, nutLegacyEntry.Fiber, nutLegacyEntry.Sodium, nutLegacyEntry.ServingSnapshotJson),
        "NUT-006 legacy: validation thất bại không đổi bất kỳ trường dinh dưỡng/khẩu phần nào của entry");
    // Legacy log theo gram đổi sang khẩu phần cũng bị từ chối (không xác minh được); entry có snapshot nhưng thiếu gram khẩu phần cũ (snapshot hỏng) cũng từ chối
    var nutLegacyGram = new FoodLogEntry { Id = Guid.NewGuid(), FoodItemId = Guid.NewGuid(), Quantity = 150m, Calories = 247.5m, Protein = 22.5m, Carbs = 30.75m, Fat = 5.25m };
    var nutBroken = NutServing(true);
    nutBroken.ServingSnapshotJson = (EntryServingSnapshot.Parse(nutBroken.ServingSnapshotJson)! with { ServingGrams = null }).ToJson();
    var nutRefusedGram = false; var nutRefusedBroken = false;
    try { EntryEdit.Plan(nutLegacyGram, 1m, nutNew, new ServingRef(nutNew, "1 chén", 100m)); } catch (BadRequestException) { nutRefusedGram = true; }
    try { EntryEdit.Plan(nutBroken, 1m, nutNew, new ServingRef(nutNew, "1 chén", 100m)); } catch (BadRequestException) { nutRefusedBroken = true; }
    Check(nutRefusedGram && nutRefusedBroken, "NUT-006 legacy: entry gram cũ và snapshot thiếu gram khẩu phần cũ cũng bị từ chối khi đổi khẩu phần");

    // Recipe: số khẩu phần gốc, sửa recipe sau đó không ảnh hưởng (Plan không nhận Recipe)
    var nutRec = new FoodLogEntry { Id = Guid.NewGuid(), RecipeId = Guid.NewGuid(), Quantity = 2m, Calories = 700m, Protein = 40m, Carbs = 80m, Fat = 22m, Fiber = null, Sodium = 900m };
    EntryServingSnapshot.Stamp(nutRec, null, null, null, 2m);
    var nutP5 = EntryEdit.Plan(nutRec, 3m, null, null);
    Check(nutP5.Macros is { Calories: 1050.00m, Protein: 60.00m, Fiber: null, Sodium: 1350.00m } && nutP5.Ratio == 1.5m, "NUT-005: entry recipe đổi 2→3 khẩu phần co giãn từ snapshot gốc, fiber null giữ null");

    // Sửa đi sửa lại 200 lần rồi về số lượng gốc: có snapshot gốc ⇒ macro về ĐÚNG giá trị gốc (không trôi); entry cũ ⇒ trôi có giới hạn (đo thực tế)
    var nutRnd = new Random(2026);
    var nutDrift = NutServing(true);
    var nutDriftLegacy = NutServing(false);
    for (var i = 0; i < 200; i++)
    {
        var q = Math.Round((decimal)(nutRnd.NextDouble() * 4.5 + 0.25), 2);
        foreach (var de in new[] { nutDrift, nutDriftLegacy })
        {
            var p = EntryEdit.Plan(de, q, nutSrv, null);
            if (p.Unchanged) continue;
            de.Quantity = q; de.Calories = p.Macros!.Calories; de.Protein = p.Macros.Protein; de.Carbs = p.Macros.Carbs; de.Fat = p.Macros.Fat; de.Fiber = p.Macros.Fiber; de.Sodium = p.Macros.Sodium; de.ServingSnapshotJson = p.SnapshotJson;
        }
    }
    foreach (var de in new[] { nutDrift, nutDriftLegacy })
    {
        var p = EntryEdit.Plan(de, 2m, nutSrv, null);
        if (!p.Unchanged) { de.Quantity = 2m; de.Calories = p.Macros!.Calories; de.Protein = p.Macros.Protein; de.Carbs = p.Macros.Carbs; de.Fat = p.Macros.Fat; de.Fiber = p.Macros.Fiber; de.Sodium = p.Macros.Sodium; }
    }
    var nutLegacyDrift = Math.Abs(nutDriftLegacy.Calories - 120m);
    Check(nutDrift.Calories == 120m && nutDrift.Protein == 6m && nutDrift.Carbs == 18m && nutDrift.Fat == 4.5m && nutDrift.Fiber == 1.20m && nutDrift.Sodium is null,
        "NUT-005: 200 lần sửa số lượng ngẫu nhiên rồi về gốc ⇒ macro đúng bằng gốc (không trôi) khi có snapshot gốc");
    Check(nutLegacyDrift < 5m, $"NUT-005 legacy: entry cũ (không snapshot gốc) sau 200 lần sửa lệch gốc {nutLegacyDrift:0.00} kcal (đã đo; chỉ entry cũ; cần snapshot gốc hoặc log lại để triệt tiêu)");

    // Snapshot gốc: round-trip, phiên bản lạ/hỏng ⇒ coi như không có (không đoán)
    var nutStamped = NutServing(true);
    var nutParsed = EntryServingSnapshot.Parse(nutStamped.ServingSnapshotJson);
    Check(nutParsed is { V: 1, ServingGrams: 30m, BaseAmount: 60m, Kcal: 120m, Sodium: null } && EntryServingSnapshot.Parse(null) is null && EntryServingSnapshot.Parse("{") is null
        && EntryServingSnapshot.Parse("{\"V\":99,\"BaseAmount\":1,\"Kcal\":1,\"Protein\":1,\"Carbs\":1,\"Fat\":1}") is null && nutStamped.ServingSnapshotJson!.Length < 2000, "NUT-005: snapshot gốc round-trip JSON (<2000 ký tự), JSON hỏng/phiên bản lạ ⇒ null");

    // Chuyển entry sang ngày khác: chỉ tổng ngày (derived) đổi, macro của entry nguyên vẹn
    var nutLogA = new FoodLog { Id = Guid.NewGuid() };
    var nutLogB = new FoodLog { Id = Guid.NewGuid() };
    var nutMove = NutGram();
    var nutOther = NutServing(true);
    nutLogA.Entries.Add(nutMove); nutLogA.Entries.Add(nutOther);
    FoodLogSupport.RecalcTotals(nutLogA);
    var nutBefore = (nutMove.Calories, nutMove.Protein, nutMove.Carbs, nutMove.Fat, nutMove.Fiber, nutMove.Sodium, nutMove.ServingSnapshotJson);
    nutLogA.Entries.Remove(nutMove); nutLogB.Entries.Add(nutMove);
    FoodLogSupport.RecalcTotals(nutLogA); FoodLogSupport.RecalcTotals(nutLogB);
    Check(nutLogA.TotalCalories == 120m && nutLogB.TotalCalories == 247.5m && nutBefore == (nutMove.Calories, nutMove.Protein, nutMove.Carbs, nutMove.Fat, nutMove.Fiber, nutMove.Sodium, nutMove.ServingSnapshotJson),
        "NUT-005: chuyển entry sang ngày khác chỉ đổi tổng ngày, snapshot dinh dưỡng của entry không đổi");
}

// ---- Dashboard Steps / Period (audit prod): BE trả chuỗi từng ngày + ngày kinh đúng; footer 1505 = trung bình các ngày CÓ dữ liệu trong 7 ngày cuối, không phải số ngày gần nhất/hôm nay ----
{
    var spToday = new DateOnly(2026, 10, 23);
    StepEntryModel Step(int daysAgo, int steps) => new(Guid.NewGuid(), spToday.AddDays(-daysAgo), steps, "Manual");
    // Chuỗi 7 ngày lịch kết thúc hôm nay (gồm hôm nay), mới nhất trước như BE trả; ngày today−7 nằm ngoài; ngày thiếu (today−2, today−5) không tính 0
    var spItems = new[] { Step(0, 1000), Step(1, 2000), Step(3, 3000), Step(4, 4000), Step(6, 6000), Step(7, 9999) }.ToList();
    var spRecent = StepSummary.Recent(spItems, spToday);
    Check(spRecent.Count == 5 && spRecent.All(i => i.LogDate > spToday.AddDays(-7) && i.LogDate <= spToday) && StepSummary.Last7DayAverage(spItems, spToday) == 3200,
        "Steps: cửa sổ 7 ngày lịch gồm hôm nay (today−6..today), ngày today−7 bị loại, trung bình = (1000+2000+3000+4000+6000)/5 = 3200 chỉ trên ngày CÓ dữ liệu (không chia 7)");
    // Footer "1505 steps": chỉ một ngày nhập (không phải hôm nay) ⇒ footer = 1505 dù ngày gần nhất/hôm nay không phải số đó; chuỗi từng ngày vẫn có trong Items
    var spOne = new[] { Step(3, 1505) }.ToList();
    var spMixed = new[] { Step(1, 1505), Step(2, 1505), Step(0, 1000) }.ToList();
    Check(StepSummary.Last7DayAverage(spOne, spToday) == 1505 && spOne.Single().LogDate == spToday.AddDays(-3) && StepSummary.Last7DayAverage(spMixed, spToday) == 1337 && spMixed.OrderByDescending(i => i.LogDate).First().Steps == 1000,
        "Steps: footer là TRUNG BÌNH các ngày có dữ liệu trong 7 ngày cuối (1 ngày 1505 ⇒ 1505; 1505,1505,1000 ⇒ 1337), không phải số ngày gần nhất (1000) hay hôm nay");
    Check(StepSummary.Last7DayAverage(Array.Empty<StepEntryModel>(), spToday) is null && StepSummary.Last7DayAverage(new[] { Step(10, 5000) }, spToday) is null, "Steps: không có ngày nào trong 7 ngày cuối ⇒ footer null ('---'), không bịa 0");
    // Múi giờ: 18:30 UTC ngày 22/10 = 01:30 ngày 23/10 ở Bangkok. Nhập "hôm nay" local (23/10) nằm trong cửa sổ khi `to` = ngày LOCAL (BE mặc định dùng UserLocale); lệch nếu client gửi `to` = ngày UTC
    var spUtcNow = DateTimeOffset.Parse("2026-10-22T18:30:00Z", System.Globalization.CultureInfo.InvariantCulture);
    var spLocalToday = UserLocale.At("Asia/Bangkok", spUtcNow, DayOfWeek.Monday).Today;
    var spUtcToday = UserLocale.At("UTC", spUtcNow, DayOfWeek.Monday).Today;
    var spEntered = new[] { new StepEntryModel(Guid.NewGuid(), spLocalToday, 1505, "Manual") };
    Check(spLocalToday == new DateOnly(2026, 10, 23) && spUtcToday == new DateOnly(2026, 10, 22) && StepSummary.Last7DayAverage(spEntered, spLocalToday) == 1505 && StepSummary.Last7DayAverage(spEntered.Select(e => e with { LogDate = e.LogDate }).Where(e => e.LogDate <= spUtcToday), spUtcToday) is null,
        "Steps: ngày nhập theo ngày LOCAL (Bangkok 23/10) nằm trong 7 ngày cuối khi to = ngày local; nếu hỏi theo ngày UTC (22/10) thì bị cắt (BE mặc định dùng UserLocale, UpsertStep cũng dùng UserLocale)");

    // Period: 30 ngày heatmap. Kỳ đang diễn ra (End null) bắt đầu 5 ngày trước ⇒ tới hôm nay; kỳ đã đóng 20–16 ngày trước; kỳ bắt đầu TRƯỚC from vẫn đóng góp các ngày nằm trong khoảng
    var pdFrom = spToday.AddDays(-29);
    var pdRanges = new[]
    {
        new PeriodRange(Guid.NewGuid(), spToday.AddDays(-5), null, null, null),
        new PeriodRange(Guid.NewGuid(), spToday.AddDays(-20), spToday.AddDays(-16), null, null),
        new PeriodRange(Guid.NewGuid(), spToday.AddDays(-35), spToday.AddDays(-28), null, null),
    };
    var pdDays = PeriodDays.Expand(pdRanges, pdFrom, spToday, spToday);
    Check(pdDays.Count == 5 + 6 + 2 && pdDays.First() == pdFrom && pdDays.Contains(spToday.AddDays(-5)) && pdDays.Contains(spToday) && !pdDays.Contains(spToday.AddDays(-15)) && pdDays.Contains(spToday.AddDays(-18))
          && pdDays.SequenceEqual(pdDays.Order()) && pdDays.Distinct().Count() == pdDays.Count && pdDays.All(d => d >= pdFrom && d <= spToday),
        "Period: days[] của 30 ngày = 2 (kỳ bắt đầu trước from: −29,−28) + 5 (kỳ đóng −20..−16) + 6 (kỳ đang diễn ra −5..hôm nay), tăng dần, không trùng, không vượt hôm nay");
    // "x days ago" = số ngày từ StartDate kỳ gần nhất tới hôm nay (local); độ dài kỳ đang diễn ra tính tới hôm nay
    var pdLast = pdRanges[0];
    Check(spToday.DayNumber - pdLast.Start.DayNumber == 5 && PeriodDays.LengthDays(pdLast, spToday) == 6 && PeriodDays.LengthDays(pdRanges[1], spToday) == 5,
        "Period: daysSinceLastStart = hôm nay − StartDate kỳ gần nhất (5), lastPeriodLengthDays gồm hai đầu (6 cho kỳ đang diễn ra)");
    // Bật một ngày (PUT /periods/days/{date}) rồi đọc lại: ngày vừa nhập có trong days[] đúng ngày đã gửi (không lệch date/status)
    var pdAfterOn = PeriodDays.SetDay([], spToday.AddDays(-2), true, spToday);
    var pdAfterOnDays = PeriodDays.Expand(pdAfterOn.Ranges, pdFrom, spToday, spToday);
    var pdAfterOff = PeriodDays.SetDay(pdAfterOn.Ranges, spToday.AddDays(-2), false, spToday);
    Check(pdAfterOn.Changed && pdAfterOnDays.SequenceEqual([spToday.AddDays(-2)]) && PeriodDays.Expand(pdAfterOff.Ranges, pdFrom, spToday, spToday).Count == 0,
        "Period: bật ngày D ⇒ days[] chứa đúng D; tắt lại ⇒ rỗng (round-trip PUT /periods/days/{date})");
    // Múi giờ: kỳ đang diễn ra tính tới ngày LOCAL (Bangkok 23/10) chứ không phải ngày UTC (22/10)
    var pdOngoing = new[] { new PeriodRange(Guid.NewGuid(), new DateOnly(2026, 10, 21), null, null, null) };
    Check(PeriodDays.Expand(pdOngoing, new DateOnly(2026, 10, 1), spLocalToday, spLocalToday).Last() == new DateOnly(2026, 10, 23) && PeriodDays.Expand(pdOngoing, new DateOnly(2026, 10, 1), spUtcToday, spUtcToday).Last() == new DateOnly(2026, 10, 22),
        "Period: kỳ đang diễn ra kéo dài tới hôm nay theo ngày local của user (UTC+7 sớm hơn UTC một ngày)");
}

// ---- Period "N days ago" (policy được duyệt): tính từ NGÀY GẦN NHẤT thực sự được ghi nhận đang kinh trên toàn lịch sử, không phải ngày bắt đầu kỳ; không dự đoán ----
{
    var plToday = new DateOnly(2026, 10, 23);
    PeriodRange R(int startAgo, int? endAgo) => new(Guid.NewGuid(), plToday.AddDays(-startAgo), endAgo is { } e ? plToday.AddDays(-e) : null, null, null);
    int? Ago(IEnumerable<PeriodRange> rs, DateOnly today) => PeriodDays.LastTrackedDay(rs, today) is { } d ? today.DayNumber - d.DayNumber : null;
    int? OldAgo(IEnumerable<PeriodRange> rs, DateOnly today) => rs.Where(r => r.Start <= today).OrderByDescending(r => r.Start).FirstOrDefault() is { } l ? today.DayNumber - l.Start.DayNumber : null;   // hành vi cũ: từ ngày BẮT ĐẦU kỳ
    // 1) Kỳ nhiều ngày liên tiếp (10 → 7 ngày trước, gồm cả ngày cuối): mới = 7 (ngày cuối được ghi nhận), cũ = 10 (ngày bắt đầu)
    var plMulti = new[] { R(10, 7) };
    Check(Ago(plMulti, plToday) == 7 && OldAgo(plMulti, plToday) == 10 && PeriodDays.LastTrackedDay(plMulti, plToday) == plToday.AddDays(-7), "Period days-ago 1: kỳ nhiều ngày liên tiếp ⇒ tính từ ngày CUỐI được ghi nhận (7), không phải ngày bắt đầu (cũ 10)");
    // 2) Ngày cuối cùng được ghi nhận của kỳ gần nhất, thứ tự đầu vào không quan trọng
    var plTwo = new[] { R(3, 2), R(40, 36) };
    Check(Ago(plTwo, plToday) == 2 && Ago(plTwo.Reverse(), plToday) == 2 && OldAgo(plTwo, plToday) == 3, "Period days-ago 2: nhiều kỳ ⇒ ngày ghi nhận gần nhất (2), độc lập thứ tự; cũ = 3 (ngày bắt đầu)");
    // 3) Lịch sử ngoài 30 và 365 ngày: vẫn tìm được (không giới hạn khoảng card/query), trong khi days[] của 30 ngày rỗng
    var plOld = new[] { R(400, 396) };
    Check(Ago(plOld, plToday) == 396 && PeriodDays.Expand(plOld, plToday.AddDays(-29), plToday, plToday).Count == 0 && PeriodDays.Expand(plOld, plToday.AddDays(-364), plToday, plToday).Count == 0 && OldAgo(plOld, plToday) == 400,
        "Period days-ago 3: lịch sử ngoài 30/365 ngày vẫn có số (396) dù days[] của khoảng đó rỗng (FE không thể tự suy)");
    // 4) Múi giờ: 18:30Z 22/10 = 23/10 ở Bangkok. Kỳ kết thúc 21/10 ⇒ cách 2 ngày local (UTC: 1 ngày)
    var plUtcNow = DateTimeOffset.Parse("2026-10-22T18:30:00Z", System.Globalization.CultureInfo.InvariantCulture);
    var plBkk = UserLocale.At("Asia/Bangkok", plUtcNow, DayOfWeek.Monday).Today; var plUtc = UserLocale.At("UTC", plUtcNow, DayOfWeek.Monday).Today;
    var plEnded = new[] { new PeriodRange(Guid.NewGuid(), new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 21), null, null) };
    Check(plBkk == new DateOnly(2026, 10, 23) && plUtc == new DateOnly(2026, 10, 22) && Ago(plEnded, plBkk) == 2 && Ago(plEnded, plUtc) == 1, "Period days-ago 4: chênh lệch tính theo NGÀY LỊCH local của user (Bangkok 2, UTC 1 cho cùng dữ liệu)");
    // 5) Ngày tương lai bị loại: kỳ chỉ ở tương lai ⇒ null; kỳ bắc cầu qua hôm nay bị kẹp ở hôm nay (0)
    var plFuture = new[] { R(-1, -3) };                 // bắt đầu ngày mai
    var plSpan = new[] { R(2, -5), R(-1, -3) };         // −2 → +5 và kỳ tương lai
    Check(Ago(plFuture, plToday) is null && Ago(plSpan, plToday) == 0 && PeriodDays.LastTrackedDay(plSpan, plToday) == plToday, "Period days-ago 5: ngày tương lai không bao giờ được tính (kỳ chỉ tương lai ⇒ null; kỳ bắc cầu bị kẹp ở hôm nay)");
    // 6) Kỳ đang diễn ra (EndDate null): hôm nay là ngày đã ghi nhận ⇒ 0 (cũ: số ngày từ ngày bắt đầu)
    var plOngoing = new[] { R(5, null) };
    Check(Ago(plOngoing, plToday) == 0 && OldAgo(plOngoing, plToday) == 5, "Period days-ago 6: kỳ đang diễn ra (EndDate null) ⇒ hôm nay là ngày ghi nhận ⇒ 0 days ago (cũ 5)");
    // 7) Không có kỳ hợp lệ ⇒ null ('---')
    Check(Ago(Array.Empty<PeriodRange>(), plToday) is null && PeriodDays.LastTrackedDay(Array.Empty<PeriodRange>(), plToday) is null, "Period days-ago 7: chưa có kỳ nào ⇒ null (FE giữ '---'), không đoán");
    // 8) Bật ngày D (PUT /periods/days/D) rồi tắt: ngày ghi nhận gần nhất theo dõi đúng thao tác của user
    var plOn = PeriodDays.SetDay([], plToday.AddDays(-4), true, plToday);
    var plOff = PeriodDays.SetDay(plOn.Ranges, plToday.AddDays(-4), false, plToday);
    Check(Ago(plOn.Ranges, plToday) == 4 && Ago(plOff.Ranges, plToday) is null, "Period days-ago 8: đánh dấu ngày −4 ⇒ 4 days ago; bỏ đánh dấu ⇒ null");
}

// ---- Body metrics: một điểm thật cho mỗi lần log; ngày có nhiều hàng (DB không chặn trùng) thì GHI/XOÁ phải nhất quán với ĐỌC; "Last 7 Entries" = 7 lần đo, không phải 7 ngày ----
{
    var bmD = new DateOnly(2026, 10, 20);
    var bmT0 = DateTimeOffset.Parse("2026-10-20T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    var bmWaist = BodyMeasurements.Find("waist")!; var bmNeck = BodyMeasurements.Find("neck")!;
    BodyMetric Tape(DateOnly d, DateTimeOffset created, decimal? waist, decimal? neck = null) => new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), MeasuredDate = d, Method = MeasurementMethod.Tape, CreatedAt = created, WaistCm = waist, NeckCm = neck };
    List<BodyMetric> Dup() => [Tape(bmD, bmT0.AddHours(2), 82m), Tape(bmD, bmT0, 80m, 38m)];   // mới nhất trước: [82 cm], [80 cm + neck 38]
    // 1) ĐỌC: mỗi số đo lấy hàng mới nhất CÓ giá trị đó trong ngày (waist 82; neck chỉ có ở hàng cũ ⇒ 38), một điểm/ngày
    var bmRead = Dup();
    Check(TapeDayRows.Collapse(bmRead, bmWaist) is [{ ValueCm: 82m }] c1 && c1[0].Date == bmD && TapeDayRows.Collapse(bmRead, bmNeck) is [{ ValueCm: 38m }], "Body 1: ngày trùng hàng ⇒ đọc một điểm/ngày, hàng mới nhất có giá trị thắng (waist 82, neck 38 từ hàng cũ)");
    // 2) GHI: ghi vào hàng MỚI NHẤT ⇒ lần đọc kế thấy đúng giá trị mới (bản cũ ghi vào hàng CŨ NHẤT nên đọc vẫn thấy 82)
    var bmSet = Dup(); var bmSetRes = TapeDayRows.Apply(bmSet, bmWaist, 81m, () => throw new InvalidOperationException());
    var bmPre = Dup(); bmWaist.Set(bmPre.Last(), 81m);   // hành vi cũ: LoadAsync lấy hàng cũ nhất
    Check(bmSetRes.Changed && bmSetRes.Added.Count == 0 && TapeDayRows.Collapse(bmSet, bmWaist)[0].ValueCm == 81m && bmWaist.Get(bmSet[1]) == 80m && TapeDayRows.Collapse(bmPre, bmWaist)[0].ValueCm == 82m,
        "Body 2: sửa số đo của ngày trùng hàng ⇒ ghi vào hàng mới nhất nên đọc thấy 81 (cũ: ghi vào hàng cũ nhất, đọc vẫn 82)");
    // 3) XOÁ: xoá ở MỌI hàng của ngày ⇒ không còn điểm ma (cũ: chỉ xoá hàng được chọn, giá trị hàng kia hiện lại); số đo khác không đổi; hàng hết ô bị bỏ
    var bmDel = Dup(); var bmDelRes = TapeDayRows.Apply(bmDel, bmWaist, null, () => throw new InvalidOperationException());
    var bmPreDel = Dup(); bmWaist.Set(bmPreDel.Last(), null);   // hành vi cũ: chỉ xoá hàng cũ nhất
    Check(bmDelRes.Changed && TapeDayRows.Collapse(bmDel, bmWaist).Count == 0 && TapeDayRows.Collapse(bmDel, bmNeck) is [{ ValueCm: 38m }] && TapeDayRows.Emptied(bmDel).Count == 1 && TapeDayRows.Collapse(bmPreDel, bmWaist) is [{ ValueCm: 82m }],
        "Body 3: xoá số đo ngày trùng hàng ⇒ xoá ở mọi hàng, không còn điểm ma (cũ: 82 hiện lại), neck giữ nguyên, hàng rỗng bị bỏ");
    // 4) Ngày chưa có hàng: ghi tạo đúng một hàng; xoá không thay đổi gì
    var bmEmpty = new List<BodyMetric>(); var bmNew = TapeDayRows.Apply(bmEmpty, bmWaist, 79m, () => Tape(bmD, bmT0, null));
    var bmEmpty2 = new List<BodyMetric>(); var bmNoop = TapeDayRows.Apply(bmEmpty2, bmWaist, null, () => throw new InvalidOperationException());
    Check(bmNew.Changed && bmNew.Added.Count == 1 && bmWaist.Get(bmEmpty[0]) == 79m && !bmNoop.Changed && bmNoop.Added.Count == 0, "Body 4: ngày chưa có hàng ⇒ ghi tạo đúng một hàng; xoá khi không có gì ⇒ không đổi (404 ở handler)");
    // 5) "Last 7 Entries" = 7 LẦN ĐO gần nhất (ngày có dữ liệu), kể cả khi cách xa hàng tháng/năm và ngày trùng hàng không chiếm chỗ; không giới hạn trong 7 ngày lịch
    var bmMany = new List<BodyMetric> { Tape(bmD, bmT0, 80m), Tape(bmD, bmT0.AddHours(1), 81m) };   // ngày D trùng 2 hàng
    foreach (var bmOff in new[] { 3, 10, 30, 90, 200, 400, 700, 900 }) bmMany.Add(Tape(bmD.AddDays(-bmOff), bmT0, 70m + bmOff % 7));
    var bmLast7 = TapeDayRows.Collapse(bmMany, bmWaist).Take(7).ToList();
    Check(bmLast7.Count == 7 && bmLast7[0].Date == bmD && bmLast7[0].ValueCm == 81m && bmLast7.Select(e => e.Date).Distinct().Count() == 7 && bmLast7[^1].Date == bmD.AddDays(-400)
          && bmLast7.Zip(bmLast7.Skip(1)).All(p => p.First.Date > p.Second.Date), "Body 5: Last 7 Entries = 7 lần đo gần nhất (7 ngày có dữ liệu khác nhau, mới nhất trước), cách nhau cả năm vẫn đủ; ngày trùng hàng chỉ tính một");
}

// ---- Body metrics: PUT/DELETE full-body đi chung đường ghi (một điểm/ngày, mới nhất; xoá ở mọi hàng trùng ngày) ----
{
    var fbD = new DateOnly(2026, 10, 20);
    var fbT0 = DateTimeOffset.Parse("2026-10-20T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    BodyMetric FbTape(DateTimeOffset created, decimal? waist, decimal? neck = null) => new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), MeasuredDate = fbD, Method = MeasurementMethod.Tape, CreatedAt = created, WaistCm = waist, NeckCm = neck };
    List<BodyMetric> FbDup() => [FbTape(fbT0.AddHours(2), 82m), FbTape(fbT0, 80m, 38m)];
    // Áp tập thay đổi như FullBodyWriter.ApplyAsync (cùng TapeDayRows.Apply + Emptied)
    (List<BodyMetric> Rows, int Added) FbApply(List<BodyMetric> rows, Dictionary<string, decimal?> changes)
    {
        var added = 0;
        foreach (var (key, value) in changes.Where(c => !string.Equals(c.Key, BodyMeasurements.VisualBodyFatKey, StringComparison.OrdinalIgnoreCase)))
            added += TapeDayRows.Apply(rows, BodyMeasurements.Find(key)!, value, () => FbTape(fbT0.AddHours(3), null)).Added.Count;
        foreach (var gone in TapeDayRows.Emptied(rows)) rows.Remove(gone);
        return (rows, added);
    }
    TapeMeasurementsModel FbModel(decimal? waistCm, decimal? neckCm = null) => FullBodySupport.ToModel(FbTape(fbT0, waistCm, neckCm));
    var fbPut = FullBodySupport.ReplaceChanges(12.5m, FbModel(79m));   // sheet chỉ còn waist 79: neck bị xoá
    var fbPutRows = FbApply(FbDup(), fbPut);
    var waist = BodyMeasurements.Find("waist")!; var neck = BodyMeasurements.Find("neck")!;
    Check(fbPut.Count == BodyMeasurements.All.Count + 1 && fbPut[BodyMeasurements.VisualBodyFatKey] == 12.5m && fbPut["waist"] == 79m && fbPut["neck"] is null && fbPut.Where(c => c.Key is not ("waist" or BodyMeasurements.VisualBodyFatKey)).All(c => c.Value is null),
        "Body 6: PUT full-body = thay cả ngày: mọi 18 số đo + Visual được gửi (ô vắng = xoá)");
    Check(fbPutRows.Added == 0 && TapeDayRows.Collapse(fbPutRows.Rows, waist) is [{ ValueCm: 79m }] && TapeDayRows.Collapse(fbPutRows.Rows, neck).Count == 0 && fbPutRows.Rows.Count == 2 && waist.Get(fbPutRows.Rows[1]) == 80m && fbPutRows.Rows.All(r => r.NeckCm is null),
        "Body 6: PUT trên ngày trùng hàng ⇒ cập nhật hàng mới nhất (waist 79; giá trị 80 của hàng cũ bị che, không hiện), neck bị xoá ở MỌI hàng (không hiện lại 38), không tạo thêm điểm");
    // DELETE full-body: xoá sạch mọi hàng của ngày
    var fbDelRows = FbApply(FbDup(), FullBodySupport.ReplaceChanges(null, null));
    Check(fbDelRows.Rows.Count == 0 && BodyMeasurements.All.All(m => TapeDayRows.Collapse(fbDelRows.Rows, m).Count == 0) && FullBodySupport.ReplaceChanges(null, null)[BodyMeasurements.VisualBodyFatKey] is null,
        "Body 7: DELETE full-body ⇒ không còn điểm nào của ngày (kể cả hàng trùng cũ hơn)");
    // PUT trên ngày chưa có hàng: tạo đúng MỘT hàng cho nhiều số đo; nhập lại cùng ngày (lần hai) cập nhật hàng đó, không tạo thêm
    var fbFirst = FbApply([], FullBodySupport.ReplaceChanges(null, FbModel(90m, 40m)));
    var fbAgain = FbApply(fbFirst.Rows, FullBodySupport.ReplaceChanges(null, FbModel(89m, 40m)));
    Check(fbFirst.Added == 1 && fbFirst.Rows.Count == 1 && fbAgain.Added == 0 && fbAgain.Rows.Count == 1 && TapeDayRows.Collapse(fbAgain.Rows, waist) is [{ ValueCm: 89m }],
        "Body 8: nhập lại cùng ngày cập nhật hàng hiện tại (waist 90 → 89), không tạo thêm hàng/điểm");
}

// ---- Macros Detail: GET /nutrients/macros/series (kcal xếp chồng P×4/F×9/C×4 + Other; trung bình mỗi ngày ĐÃ LOG; % kcal) ----
{
    var mcD0 = new DateOnly(2026, 10, 19);   // Thứ 2
    FoodLogSupport.LoggedDay Mc(int off, decimal kcal, decimal p, decimal c, decimal f, bool incomplete = false, bool fasting = false) => new(mcD0.AddDays(off), kcal, p, c, f, 0m, 0m, incomplete, fasting);
    var mcLogs = new[]
    {
        Mc(0, 2000m, 150m, 200m, 60m),            // P600 + C800 + F540 = 1940 ⇒ Other +60
        Mc(1, 500m, 0m, 0m, 0m),                  // quick-add chỉ kcal ⇒ Other = 500
        Mc(2, 1000m, 100m, 150m, 40m),            // macro 1360 > kcal 1000 ⇒ Other −360 (thô có dấu, không kẹp)
        Mc(3, 0m, 0m, 0m, 0m, fasting: true),     // nhịn ăn: đã log, toàn 0
        Mc(4, 1800m, 100m, 100m, 50m, incomplete: true),   // chưa đủ: hiện số nhưng không vào thống kê
        // ngày 5: chưa log
        Mc(6, 2400m, 180m, 250m, 70m),            // P720 + C1000 + F630 = 2350 ⇒ Other +50
    }.ToDictionary(l => l.Date);
    var mcDays = MacroSeriesCalculator.Days(mcD0, mcD0.AddDays(6), mcLogs);
    Check(mcDays.Count == 7 && mcDays.Select(d => d.Date).SequenceEqual(Enumerable.Range(0, 7).Select(i => mcD0.AddDays(i)))
          && mcDays[0] is { IsLogged: true, CaloriesKcal: 2000m, ProteinKcal: 600m, CarbsKcal: 800m, FatKcal: 540m, OtherKcal: 60m }
          && mcDays[1] is { OtherKcal: 500m, ProteinG: 0m } && mcDays[2].OtherKcal == -360m,
        "Macros: ngày liên tục; kcal xếp chồng P×4 / C×4 / F×9 + Other = kcal − macro (quick-add kcal-only ⇒ Other 500; macro > kcal ⇒ −360 thô, không kẹp)");
    Check(mcDays[3] is { IsLogged: true, IsFasting: true, CaloriesKcal: 0m, OtherKcal: 0m } && mcDays[4] is { IsIncomplete: true, CaloriesKcal: 1800m } && mcDays[5] is { IsLogged: false, CaloriesKcal: null, ProteinG: null, OtherKcal: null },
        "Macros: nhịn ăn = logged 0; chưa đủ có số nhưng cờ IsIncomplete; ngày chưa log = null (không bịa 0)");
    var mcSum = MacroSeriesCalculator.Summary(mcDays, mcD0, mcD0.AddDays(6)).Averages;
    var mcSumAll = MacroSeriesCalculator.Summary(mcDays, mcD0, mcD0.AddDays(6));
    Check(mcSumAll.LoggedDays == 5 && mcSumAll.CalendarDays == 7 && mcSum.CaloriesKcal == 1180m && mcSum.CaloriesKcal != Math.Round(5900m / 7m, 2) && mcSum.ProteinG == 86m && mcSum.FatG == 34m && mcSum.CarbsG == 120m
          && mcSum.ProteinKcal == 344m && mcSum.FatKcal == 306m && mcSum.CarbsKcal == 480m && mcSum.OtherKcal == 50m,
        "Macros: trung bình MỖI NGÀY ĐÃ LOG (5 ngày: 1180 kcal), không chia cho 7 ngày lịch (842.86); ngày chưa đủ và chưa log bị loại, nhịn ăn tính là ngày log 0");
    Check(mcSum.ProteinPercent == 29.2m && mcSum.FatPercent == 25.9m && mcSum.CarbsPercent == 40.7m && mcSum.OtherPercent == 4.2m && mcSum.ProteinPercent + mcSum.FatPercent + mcSum.CarbsPercent + mcSum.OtherPercent == 100m,
        "Macros: % kcal của từng phần = trung bình kcal phần ÷ trung bình kcal tổng (29.2 / 25.9 / 40.7 / 4.2 = 100)");
    // Khoảng (1W/1M/3M… do from/to): chỉ ngày trong [from,to] vào thống kê
    var mcRange = MacroSeriesCalculator.Summary(mcDays, mcD0.AddDays(1), mcD0.AddDays(6));
    Check(mcRange.LoggedDays == 4 && mcRange.Averages.CaloriesKcal == 975m && mcRange.CalendarDays == 6, "Macros: khoảng con [ngày 1, ngày 6]: 4 ngày đã log, trung bình 975 kcal");
    // Bucket: tuần (Thứ 2 bắt đầu) gom 5 ngày; Day = từng ngày hợp lệ; chỉ bucket có ngày đã log
    var mcWeek = MacroSeriesCalculator.Buckets(mcDays, MetricGranularity.Week, DayOfWeek.Monday);
    var mcDay = MacroSeriesCalculator.Buckets(mcDays, MetricGranularity.Day, DayOfWeek.Monday);
    Check(mcWeek.Count == 1 && mcWeek[0].Start == mcD0 && mcWeek[0].End == mcD0.AddDays(6) && mcWeek[0].LoggedDays == 5 && mcWeek[0].Averages.CaloriesKcal == 1180m && mcDay.Count == 5 && mcDay.All(b => b.LoggedDays == 1),
        "Macros: bucket Week/Day cùng quy tắc ngày đã log (Week 5 ngày, Day 5 bucket; ngày chưa đủ/chưa log vắng)");
    // Không có dữ liệu: null (không bịa 0); chỉ có nhịn ăn: trung bình 0 nhưng % null (không chia cho 0)
    var mcNone = MacroSeriesCalculator.Summary(MacroSeriesCalculator.Days(mcD0, mcD0.AddDays(2), new Dictionary<DateOnly, FoodLogSupport.LoggedDay>()), mcD0, mcD0.AddDays(2));
    var mcFast = MacroSeriesCalculator.Summary(MacroSeriesCalculator.Days(mcD0, mcD0, new Dictionary<DateOnly, FoodLogSupport.LoggedDay> { [mcD0] = Mc(0, 0m, 0m, 0m, 0m, fasting: true) }), mcD0, mcD0);
    Check(mcNone.LoggedDays == 0 && mcNone.Averages is { CaloriesKcal: null, ProteinPercent: null, OtherKcal: null } && mcFast.LoggedDays == 1 && mcFast.Averages is { CaloriesKcal: 0m, ProteinPercent: null, OtherPercent: null },
        "Macros: không có ngày log ⇒ mọi trung bình null; chỉ nhịn ăn ⇒ trung bình 0 và % null (không chia cho 0)");
}

// ---- Onboarding weight: không ghi đè / xoá ghi chú của lần cân hôm nay; chỉ tạo khi chưa có; một nguồn sự thật (lần cân đã lưu) ----
{
    // chưa có lần cân hôm nay ⇒ tạo từ số user nhập; câu trả lời Prior Weight dùng số đó
    var owNone = OnboardingWeight.Decide(82.4m, null, 85m);
    // đã có lần cân hôm nay (cùng số hoặc khác số, có thể có ghi chú) ⇒ KHÔNG ghi (không ghi đè, không xoá ghi chú, không dòng thứ hai); mọi nơi dùng số ĐÃ LƯU
    var owSame = OnboardingWeight.Decide(82.4m, 82.4m, 82.4m);
    var owDiff = OnboardingWeight.Decide(79.0m, 82.4m, 82.4m);
    // gửi lại/retry sau khi đã tạo ⇒ lần hai đã có ⇒ không ghi
    var owRetry = OnboardingWeight.Decide(82.4m, 82.4m, 82.4m);
    // không gửi cân nặng ⇒ không ghi; Prior Weight dùng lần cân gần nhất; không có gì ⇒ null (không bịa)
    var owNoInput = OnboardingWeight.Decide(null, null, 85m);
    var owNothing = OnboardingWeight.Decide(null, null, null);
    Check(owNone is { Write: true, WeightAtAnswer: 82.4m } && owSame is { Write: false, WeightAtAnswer: 82.4m } && owDiff is { Write: false, WeightAtAnswer: 82.4m } && owRetry.Write == false
          && owNoInput is { Write: false, WeightAtAnswer: 85m } && owNothing is { Write: false, WeightAtAnswer: null },
        "Onboarding weight: chưa có entry hôm nay ⇒ tạo; đã có (cùng/khác số, kể cả có Notes) ⇒ KHÔNG ghi và mọi nơi dùng số đã lưu (82.4, không phải 79); retry ⇒ không ghi; không nhập ⇒ không ghi");
    // Ngày hôm nay theo múi giờ user (không phải ngày UTC): 18:30Z 22/10 = 23/10 ở Bangkok; entry của ngày 23/10 là "hôm nay" của user ⇒ nhận ra entry đã có; entry 22/10 thì chưa
    var owNow = DateTimeOffset.Parse("2026-10-22T18:30:00Z", System.Globalization.CultureInfo.InvariantCulture);
    var owToday = UserLocale.At("Asia/Bangkok", owNow, DayOfWeek.Monday).Today;
    var owEntries = new[] { (Date: new DateOnly(2026, 10, 22), Kg: 81m) };   // chỉ có cân của ngày UTC, chưa có của ngày local
    decimal? owStored = owEntries.Where(e => e.Date == owToday).Select(e => (decimal?)e.Kg).FirstOrDefault();
    decimal? owStoredUtc = owEntries.Where(e => e.Date == UserLocale.At("UTC", owNow, DayOfWeek.Monday).Today).Select(e => (decimal?)e.Kg).FirstOrDefault();
    Check(owToday == new DateOnly(2026, 10, 23) && OnboardingWeight.Decide(82.4m, owStored, 81m).Write && !OnboardingWeight.Decide(82.4m, owStoredUtc, 81m).Write,
        "Onboarding weight: 'hôm nay' theo múi giờ user (Bangkok 23/10): entry ngày UTC 22/10 không chặn việc tạo entry hôm nay; nếu dùng ngày UTC sẽ nhận nhầm và bỏ ghi");

    // weightKept: CHỈ khi có cân nặng gửi lên + hôm nay đã có lần cân + số khác nhau (so theo số hiển thị 1 chữ số thập phân, đơn vị user)
    var owkMetric = UnitSystem.Metric; var owkImp = UnitSystem.Imperial;
    Check(!OnboardingWeight.Kept(82.4m, 82.4m, owkMetric) && OnboardingWeight.Kept(79.0m, 82.4m, owkMetric) && !OnboardingWeight.Kept(82.4m, null, owkMetric) && !OnboardingWeight.Kept(null, 82.4m, owkMetric) && !OnboardingWeight.Kept(null, null, owkMetric),
        "weightKept: trùng số ⇒ false; khác số ⇒ true; chưa có entry (sẽ tạo) ⇒ false; không gửi cân nặng ⇒ false");
    // dung sai 0.1 theo đơn vị hiển thị: lệch nhỏ hơn mức làm tròn 1 chữ số không báo; từ 0.1 trở lên thì báo
    Check(!OnboardingWeight.Kept(82.42m, 82.4m, owkMetric) && OnboardingWeight.Kept(82.5m, 82.4m, owkMetric) && OnboardingWeight.Kept(82.3m, 82.4m, owkMetric),
        "weightKept: kg lệch 0.02 (cùng số hiển thị 82.4) ⇒ không báo; lệch 0.1 ⇒ báo");
    // lb: nhập 181.7 lb (FE quy ra ≈82.42 kg) so với 82.4 kg đã lưu = cùng số hiển thị 181.7 lb ⇒ không báo giả; 182.0 lb ≈ 82.55 kg ⇒ báo; số hiển thị trả về theo lb
    var owkLb = 181.7m / PriorWeightQuestion.PoundsPerKg;
    Check(!OnboardingWeight.Kept(owkLb, 82.4m, owkImp) && OnboardingWeight.Kept(182.0m / PriorWeightQuestion.PoundsPerKg, 82.4m, owkImp) && OnboardingWeight.Display(82.4m, owkImp) == 181.7m && OnboardingWeight.Display(82.4m, owkMetric) == 82.4m,
        "weightKept: đơn vị lb so theo số hiển thị lb (181.7 lb vs 82.4 kg ⇒ không báo giả; 182.0 lb ⇒ báo); Display(82.4 kg) = 181.7 lb / 82.4 kg");
    // Nhiều lần submit (giữ nguyên bước, quay lại, retry) trên "kho" một dòng/ngày: lần 1 tạo, lần 2 cùng số im lặng, lần 3 khác số báo, lần 4 cùng số im lặng; luôn đúng 1 dòng, số đã lưu không đổi, Notes không bị đụng
    var owkStore = new List<(decimal Kg, string? Notes)>();
    (bool Kept, decimal? Stored) OwkSubmit(decimal? typed)
    {
        decimal? stored = owkStore.Count > 0 ? owkStore[0].Kg : null;
        if (OnboardingWeight.Decide(typed, stored, null).Write) { owkStore.Add((typed!.Value, null)); stored = owkStore[0].Kg; }
        return (OnboardingWeight.Kept(typed, stored, owkMetric), typed is null ? null : stored);
    }
    var owk1 = OwkSubmit(82.4m);
    owkStore[0] = (82.4m, "sáng nay, sau khi đi vệ sinh");   // user thêm Notes ở màn Weight
    var owk2 = OwkSubmit(82.4m); var owk3 = OwkSubmit(79.0m); var owk4 = OwkSubmit(82.4m); var owk5 = OwkSubmit(null);
    Check(owk1 is { Kept: false, Stored: 82.4m } && owk2 is { Kept: false, Stored: 82.4m } && owk3 is { Kept: true, Stored: 82.4m } && owk4.Kept == false && owk5 is { Kept: false, Stored: null }
          && owkStore.Count == 1 && owkStore[0].Kg == 82.4m && owkStore[0].Notes == "sáng nay, sau khi đi vệ sinh",
        "weightKept: submit nhiều lần (tạo ⇒ cùng số ⇒ khác số ⇒ cùng số ⇒ không cân): chỉ lần khác số báo kept, luôn 1 dòng, số đã lưu 82.4 và Notes giữ nguyên");
}

// ---- Goal start weight (Goal.StartWeightKg = TREND tại lúc bắt đầu): người dùng mới, có lịch sử, Scale ≠ Trend, tạo Program mới ----
{
    var gsTrendDays = new AlgorithmOptions().TrendDays;
    var gsDay = new DateOnly(2026, 10, 1);
    // Người dùng MỚI: đúng một lần cân ⇒ trend = chính số cân đó (điểm khởi đầu, không làm mượt, không bịa chuỗi), nhưng KHÔNG đủ dữ liệu: cổng tốc độ/ETA chặn, tiến độ 0 %, tuần đầu delta 0
    var gsOne = WeightTrendCalculator.Compute([(gsDay, 82.4m)], gsTrendDays);
    var gsOneGate = WeightTrendSeries.RateGate([gsDay], gsDay).Reason;
    var gsOneEta = GoalProgressCalculator.Eta([(gsDay, gsOne[gsDay])], 75m, GoalDirection.Loss, gsDay, gsOneGate);
    var gsOneWeeks = GoalProgressCalculator.Weeks(gsDay, gsOne[gsDay], [(gsDay, gsOne[gsDay])], gsDay);
    Check(gsOne.Count == 1 && gsOne[gsDay] == 82.4m && gsOneGate == WeightTrendSeries.InsufficientWeightData
          && gsOneEta is { Date: null, Status: "Unavailable", ReasonCode: "INSUFFICIENT_WEIGHT_DATA" }
          && GoalProgressCalculator.ProgressPercent(gsOne[gsDay], 75m, gsOne[gsDay]) == 0m && gsOneWeeks is [{ DeltaKg: 0m }],
        "Goal start: người dùng mới (1 lần cân) ⇒ trend = số cân, KHÔNG có tốc độ/ETA (INSUFFICIENT_WEIGHT_DATA), tiến độ 0 %, tuần đầu delta 0; không coi là đã có chuỗi trend đáng tin");

    // Người dùng CÓ LỊCH SỬ, Scale ≠ Trend: 14 ngày giảm 0.2/ngày, hôm cuối (d13) cân hụt xuống 86.0 (nhiễu); trend LAG cân thô
    var gsScale = Enumerable.Range(0, 14).Select(i => (Date: gsDay.AddDays(i), Kg: i == 13 ? 86.0m : 90m - 0.2m * i)).ToList();
    var gsHist = WeightTrendCalculator.Compute(gsScale.Select(s => (s.Date, s.Kg)), gsTrendDays);
    var gsLast = gsDay.AddDays(13);
    var gsStart = gsHist[gsLast];                                   // StartWeightKg khi tạo goal = trend mới nhất (không phải cân thô mới nhất)
    Check(gsStart > 86.0m && gsStart != gsScale[^1].Kg && gsStart < 90m, "Goal start: lịch sử có nhiễu ⇒ StartWeightKg lấy trend (> cân thô 86.0 của hôm cuối), không lấy cân thô mới nhất");
    // Goal bắt đầu d14, có lần cân d14 = 85.8: tuần đầu so với StartWeightKg (trend) cho delta nhỏ ÂM; nếu nhầm Start = cân thô 86.0 sẽ ra delta DƯƠNG lớn (trend vẫn > 86)
    var gsNext = gsDay.AddDays(14);
    var gsAll = WeightTrendCalculator.Compute(gsScale.Select(s => (s.Date, s.Kg)).Append((gsNext, 85.8m)), gsTrendDays);
    var gsWeeks = GoalProgressCalculator.Weeks(gsNext, gsStart, [(gsNext, gsAll[gsNext])], gsNext);
    var gsWrong = GoalProgressCalculator.Weeks(gsNext, gsScale[^1].Kg, [(gsNext, gsAll[gsNext])], gsNext);
    Check(gsWeeks[0].DeltaKg is < 0m and > -1m && gsWrong[0].DeltaKg > 0m, "Goal start: Start = trend ⇒ delta tuần đầu nhỏ âm; Start = cân thô (sai) ⇒ delta dương giả");
    // Hoàn thành mục tiêu hiện theo TREND: cân thô đã chạm 87.0 nhưng trend còn trên ⇒ KHÔNG GoalAchieved (hành vi hiện tại, chưa có Goal Tracking Mode)
    var gsHistPoints = gsScale.Select(s => (s.Date, gsHist[s.Date])).ToList();
    var gsHistGate = WeightTrendSeries.RateGate(gsScale.Select(s => s.Date), gsLast).Reason;
    Check(gsScale[^1].Kg <= 87.0m && gsHist[gsLast] > 87.0m && GoalProgressCalculator.Eta(gsHistPoints, 87.0m, GoalDirection.Loss, gsLast, gsHistGate).Status != "GoalAchieved" && gsHistGate is null,
        "Goal tracking hiện tại: hoàn thành xét theo TREND (cân thô 86.0 ≤ 87 nhưng trend > 87 ⇒ chưa đạt); 14 ngày cân liên tục qua cổng dữ liệu");

    // Tạo Program/Goal mới KHÔNG đổi lịch sử trend: trend các ngày ≤ d13 y nguyên khi có thêm lần cân sau đó ⇒ Start đã chốt không bị viết lại bởi dữ liệu mới
    Check(gsScale.All(s => gsAll[s.Date] == gsHist[s.Date]) && gsAll.Count == gsHist.Count + 1,
        "Goal start: thêm lần cân mới không viết lại trend các ngày trước (StartWeightKg đã chốt giữ nguyên, lịch sử trend không bị reset)");
}

// ---- Flux (MACROOS_FLUX_V1 ĐỀ XUẤT, CHƯA nối production): hàm thuần, bao luôn chứa TDEE đã học, nhân quả, Calibrating/Holding, độ rộng do dữ liệu quyết định ----
{
    var flxStart = new DateOnly(2026, 3, 1);
    // Vector tính tay: kcal 2000 mỗi ngày, trend 80.0 − 0.1/ngày (ngày nào cũng cân), TDEE₀ 2500, β 0.5, W 21 ⇒ raw mỗi ngày = 2000 + 0.1 × 7700 = 2770
    var flxObs = Enumerable.Range(0, 12).Select(i => new ExpenditureDay(flxStart.AddDays(i), 2000m, 80.0m - 0.1m * i)).ToList();
    var flxLearned = Enumerable.Range(0, 12).Select(i => new FluxLearnedDay(flxStart.AddDays(i), 2500m + i, i == 0 ? ExpenditureStatus.Calibrating : ExpenditureStatus.Updating)).ToList();
    var flxPath = ExpenditureFluxCalculator.Trajectory(flxObs, flxLearned, flxStart, new FluxCandidateSpec("t", 0.5m, 21), 7700);
    Check(flxPath[0] == 2500m && flxPath[1] == 2635m && flxPath[2] == 2702.5m && flxPath[3] == 2736.25m,
        "Flux: quỹ đạo ứng viên khớp vector tính tay (2500 → 2635 → 2702.5 → 2736.25), dùng ĐÚNG ComputeRaw/Smooth của production");

    var flxBand = ExpenditureFluxCalculator.Compute(flxObs, flxLearned, flxStart, FluxCalculationPolicy.V1, 7700);
    Check(flxBand[0] is { State: FluxBandState.Calibrating, LowerKcal: null, UpperKcal: null }
          && flxBand.Skip(1).Zip(flxLearned.Skip(1)).All(p => p.First.State == FluxBandState.Updating && p.First.LowerKcal <= p.Second.TdeeKcal && p.Second.TdeeKcal <= p.First.UpperKcal),
        "Flux: Calibrating = không có dải (null); các ngày Updating luôn Lower ≤ TDEE đã học ≤ Upper");

    // Holding: không có quan sát mới ⇒ ứng viên không cập nhật ⇒ dải đứng yên; chưa từng Updating ⇒ không có dải
    var flxHold = flxLearned.Select((l, i) => i >= 6 ? l with { TdeeKcal = flxLearned[5].TdeeKcal, Status = ExpenditureStatus.Holding } : l).ToList();
    var flxHoldBand = ExpenditureFluxCalculator.Compute(flxObs, flxHold, flxStart, FluxCalculationPolicy.V1, 7700);
    var flxNever = flxLearned.Select(l => l with { Status = ExpenditureStatus.Holding }).ToList();
    Check(flxHoldBand.Skip(5).All(p => p.LowerKcal == flxHoldBand[5].LowerKcal && p.UpperKcal == flxHoldBand[5].UpperKcal) && flxHoldBand.Skip(6).All(p => p.State == FluxBandState.Holding)
          && ExpenditureFluxCalculator.Compute(flxObs, flxNever, flxStart, FluxCalculationPolicy.V1, 7700).All(p => p.LowerKcal is null && p.State == FluxBandState.NoObservations),
        "Flux: Holding giữ nguyên dải ngày cuối (không quan sát mới ⇒ không cập nhật); chuỗi chưa từng Updating ⇒ NoObservations, không dải");

    // Nhân quả: cắt dữ liệu ở ngày 8 cho đúng cùng dải ở các ngày ≤ 8; xác định: chạy hai lần như nhau
    var flxCut = ExpenditureFluxCalculator.Compute(flxObs.Take(9).ToList(), flxLearned.Take(9).ToList(), flxStart, FluxCalculationPolicy.V1, 7700);
    var flxAgain = ExpenditureFluxCalculator.Compute(flxObs, flxLearned, flxStart, FluxCalculationPolicy.V1, 7700);
    Check(flxCut.Zip(flxBand).All(p => p.First.LowerKcal == p.Second.LowerKcal && p.First.UpperKcal == p.Second.UpperKcal) && flxBand.Zip(flxAgain).All(p => p.First.LowerKcal == p.Second.LowerKcal && p.First.UpperKcal == p.Second.UpperKcal),
        "Flux: nhân quả (cắt dữ liệu ở ngày 8 cho cùng dải) và xác định (chạy hai lần như nhau)");

    // Độ rộng do DỮ LIỆU quyết định (không phải hằng ± kcal / cũ ±37): cùng chuỗi đã học, quan sát khác ⇒ độ rộng khác; không bằng 74 của dải cũ
    var flxNoisy = Enumerable.Range(0, 12).Select(i => new ExpenditureDay(flxStart.AddDays(i), i % 2 == 0 ? 1500m : 2600m, 80.0m - 0.1m * i + (i % 3 == 0 ? 0.4m : 0m))).ToList();
    var flxNoisyBand = ExpenditureFluxCalculator.Compute(flxNoisy, flxLearned, flxStart, FluxCalculationPolicy.V1, 7700);
    var flxWidthA = flxBand[^1].UpperKcal - flxBand[^1].LowerKcal; var flxWidthB = flxNoisyBand[^1].UpperKcal - flxNoisyBand[^1].LowerKcal;
    Check(flxWidthA != flxWidthB && flxWidthA != 74m && flxWidthB != 74m, $"Flux: độ rộng phụ thuộc quan sát ({flxWidthA} vs {flxWidthB} kcal), không phải hằng ±37 của dải cũ");

    // Bộ giải quan sát dùng chung: chưa log = null (không 0); log chưa đủ = null; 0 kcal không phải nhịn ăn = null; nhịn ăn = 0 hợp lệ; trend chỉ ở ngày có cân
    var flxLogs = new Dictionary<DateOnly, FoodLogSupport.LoggedDay>
    {
        [flxStart] = new(flxStart, 1800m, 0, 0, 0, 0, 0),
        [flxStart.AddDays(1)] = new(flxStart.AddDays(1), 900m, 0, 0, 0, 0, 0, IsIncomplete: true),
        [flxStart.AddDays(2)] = new(flxStart.AddDays(2), 0m, 0, 0, 0, 0, 0, IsFasting: true),
        [flxStart.AddDays(3)] = new(flxStart.AddDays(3), 0m, 0, 0, 0, 0, 0),
    };
    var flxResolved = ExpenditureObservationResolver.Daily(flxStart, flxStart.AddDays(4), flxLogs, new Dictionary<DateOnly, decimal> { [flxStart] = 80m });
    Check(flxResolved.Select(d => d.KcalIn).SequenceEqual(new decimal?[] { 1800m, null, 0m, null, null }) && flxResolved[0].TrendKg == 80m && flxResolved.Skip(1).All(d => d.TrendKg is null),
        "Flux observation resolver: kcal 1800 / incomplete → null / fasting → 0 / 0 kcal không nhịn ăn → null / chưa log → null; trend chỉ ở ngày có cân (nội suy không phải quan sát)");
}

// ---- Body metrics: một giá trị / (user, ngày, metric); ghi lại thay thế, metric khác không bị đụng; ghi đồng thời idempotent ----
{
    var umD = new DateOnly(2026, 10, 20);
    BodyMetric UmRow(DateOnly d, decimal? neck = null, decimal? waist = null) => new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), MeasuredDate = d, Method = MeasurementMethod.Tape, CreatedAt = DateTimeOffset.UtcNow, NeckCm = neck, WaistCm = waist };
    var umNeck = BodyMeasurements.Find("neck")!; var umWaist = BodyMeasurements.Find("waist")!;
    // Neck 10 rồi Neck 19 cùng ngày ⇒ MỘT hàng, giá trị 19, không điểm thứ hai; waist đã có không bị đụng
    var umRows = new List<BodyMetric> { UmRow(umD, neck: 10m, waist: 80m) };
    var umEdit = TapeDayRows.Apply(umRows, umNeck, 19m, () => throw new InvalidOperationException());
    Check(umEdit.Changed && umEdit.Added.Count == 0 && umRows.Count == 1 && umNeck.Get(umRows[0]) == 19m && umWaist.Get(umRows[0]) == 80m && TapeDayRows.Collapse(umRows, umNeck) is [{ ValueCm: 19m }],
        "Body unique: Neck 10 → 19 cùng ngày ⇒ một hàng/một điểm = 19, waist 80 giữ nguyên");
    // Hai metric KHÁC nhau cùng ngày: cùng một hàng Tape (hai cột), không xoá/ghi đè nhau; ghi lần đầu tạo đúng một hàng
    var umFresh = new List<BodyMetric>();
    var umFirst = TapeDayRows.Apply(umFresh, umNeck, 38m, () => UmRow(umD));
    var umSecond = TapeDayRows.Apply(umFresh, umWaist, 82m, () => UmRow(umD));
    Check(umFirst.Added.Count == 1 && umSecond.Added.Count == 0 && umFresh.Count == 1 && umNeck.Get(umFresh[0]) == 38m && umWaist.Get(umFresh[0]) == 82m,
        "Body unique: ghi lần đầu tạo một hàng; metric khác cùng ngày chỉ thêm cột vào hàng đó (neck 38 + waist 82)");
    // Xoá một metric giữ metric khác; xoá hết thì hàng rỗng bị bỏ
    var umDelNeck = TapeDayRows.Apply(umFresh, umNeck, null, () => throw new InvalidOperationException());
    var umEmptiedAfterNeck = TapeDayRows.Emptied(umFresh).Count;
    var umKeepsWaist = umNeck.Get(umFresh[0]) is null && umWaist.Get(umFresh[0]) == 82m;
    TapeDayRows.Apply(umFresh, umWaist, null, () => throw new InvalidOperationException());
    Check(umDelNeck.Changed && umKeepsWaist && umEmptiedAfterNeck == 0 && TapeDayRows.Emptied(umFresh).Count == 1, "Body unique: xoá neck giữ waist; xoá cả hai ⇒ hàng rỗng bị bỏ");
    // Sửa ngày quá khứ: chỉ ngày đó đổi, các ngày khác nguyên vẹn
    var umHist = new List<BodyMetric> { UmRow(umD, neck: 37m), UmRow(umD.AddDays(-7), neck: 36m), UmRow(umD.AddDays(-30), neck: 35m) };
    TapeDayRows.Apply(umHist.Where(r => r.MeasuredDate == umD.AddDays(-7)).ToList(), umNeck, 36.5m, () => throw new InvalidOperationException());
    Check(TapeDayRows.Collapse(umHist, umNeck).Select(e => e.ValueCm).SequenceEqual([37m, 36.5m, 35m]), "Body unique: sửa ngày quá khứ chỉ đổi điểm của ngày đó (37, 36.5, 35)");

    // Ghi đồng thời: bên thua (unique violation) chạy lại ĐÚNG một lần như cập nhật; hết lần thì 409; lỗi khác không bị nuốt
    var umAttempts = 0; var umDiscards = 0;
    var umOk = await IdempotentWrite.RunAsync(() => { umAttempts++; return Task.FromResult(7); }, () => umDiscards++);
    var okAttempts = umAttempts; var okDiscards = umDiscards;
    umAttempts = 0; umDiscards = 0;
    var umRetried = await IdempotentWrite.RunAsync(() => { umAttempts++; if (umAttempts == 1) throw new Microsoft.EntityFrameworkCore.DbUpdateException("unique"); return Task.FromResult(9); }, () => umDiscards++);
    var retryAttempts = umAttempts; var retryDiscards = umDiscards;
    umAttempts = 0; umDiscards = 0;
    ConflictException? umConflict = null;
    try { await IdempotentWrite.RunAsync<int>(() => { umAttempts++; throw new Microsoft.EntityFrameworkCore.DbUpdateException("unique"); }, () => umDiscards++); } catch (ConflictException e) { umConflict = e; }
    var conflictAttempts = umAttempts; var conflictDiscards = umDiscards;
    umAttempts = 0;
    var umOther = false;
    try { await IdempotentWrite.RunAsync<int>(() => { umAttempts++; throw new InvalidOperationException("boom"); }, () => umDiscards++); } catch (InvalidOperationException) { umOther = true; }
    Check(umOk == 7 && okAttempts == 1 && okDiscards == 0 && umRetried == 9 && retryAttempts == 2 && retryDiscards == 1 && umConflict is { StatusCode: 409 } && conflictAttempts == 2 && conflictDiscards == 2 && umOther && umAttempts == 1,
        "Body unique: ghi đồng thời — thành công ngay = 1 lần; bên thua unique violation chạy lại đúng 1 lần (bỏ phần chưa lưu) và thành công; vẫn thua ⇒ 409; lỗi khác không bị nuốt/không retry");

    // "Double edit PUT /food-logs/entries/{id}" (500): hai request cùng đọc RowVersion của FoodLogEntry/FoodLog ⇒ bên lưu sau ném DbUpdateConcurrencyException (con của DbUpdateException).
    // Handler PUT giờ chạy trong IdempotentWrite: bên thua bỏ phần chưa lưu, đọc lại, áp lại đúng một lần (last writer wins); vẫn thua ⇒ 409, không 500.
    var ccrAttempts = 0; var ccrDiscards = 0;
    var ccrRetried = await IdempotentWrite.RunAsync(() => { ccrAttempts++; if (ccrAttempts == 1) throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("rowversion"); return Task.FromResult(5); }, () => ccrDiscards++);
    var ccrFirst = (ccrAttempts, ccrDiscards, ccrRetried);
    ccrAttempts = 0; ccrDiscards = 0; ConflictException? ccrConflict = null;
    try { await IdempotentWrite.RunAsync<int>(() => { ccrAttempts++; throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("rowversion"); }, () => ccrDiscards++); } catch (ConflictException e) { ccrConflict = e; }
    Check(ccrFirst == (2, 1, 5) && ccrConflict is { StatusCode: 409 } && ccrAttempts == 2 && ccrDiscards == 2,
        "Double PUT entry: DbUpdateConcurrencyException (RowVersion) được IdempotentWrite xử lý như xung đột ghi: chạy lại đúng 1 lần và thành công; vẫn xung đột ⇒ ConflictException 409 (không 500)");
}

// ---- EXP-001: lượng ăn và Δtrend phải cùng khoảng thời gian [lần cân đầu, lần cân cuối] (vector tính tay/Python độc lập, không dùng công thức của code) ----
{
    var e0 = new DateOnly(2026, 1, 1);
    List<ExpenditureDay> Win(int n, Func<int, decimal?> kcal, Func<int, decimal?> trend) => Enumerable.Range(0, n).Select(i => new ExpenditureDay(e0.AddDays(i), kcal(i), trend(i))).ToList();
    decimal? W(int i, params (int Day, decimal Kg)[] w) => w.Where(x => x.Day == i).Select(x => (decimal?)x.Kg).FirstOrDefault();

    // EXP-001 gốc: ăn 3000 ngày 0-13, 2000 ngày 14-20; cân ngày 14 (80.00) và 20 (79.55): đúng = 2000 + 0.45·7700/6 = 2577.5 (cũ 3244.17 vì lấy trung bình 21 ngày = 2666.67)
    var x1 = ExpenditureCalculator.ComputeRaw(Win(21, i => i < 14 ? 3000m : 2000m, i => W(i, (14, 80.00m), (20, 79.55m))), 7700);
    Check(x1 is { TdeeRaw: 2577.5m, AvgKcalIn: 2000m, LoggedDays: 7, RateKgPerWeek: -0.525m }, "EXP-001: ăn ngoài khoảng cân (ngày 0-13) không còn lọt vào TDEE: 2577.5 (cũ 3244.17), chỉ 7 ngày trong khoảng, avg 2000");
    // Hồi quy: cân mỗi ngày (trend 80→79 tuyến tính) và chỉ cân 2 đầu cửa sổ, ăn 2200 cả 21 ngày: cả hai = 2200 + 1.0·7700/20 = 2585 (cửa sổ khớp thì kết quả không đổi so với trước)
    var xDaily = ExpenditureCalculator.ComputeRaw(Win(21, i => 2200m, i => 80m - i * 0.05m), 7700);
    var xEnds = ExpenditureCalculator.ComputeRaw(Win(21, i => 2200m, i => W(i, (0, 80m), (20, 79m))), 7700);
    Check(xDaily?.TdeeRaw == 2585m && xEnds?.TdeeRaw == 2585m && xDaily.LoggedDays == 21, "EXP-001 hồi quy: cân mỗi ngày và cân không liên tục (2 đầu) đều 2585, không đổi khi cửa sổ khớp khoảng cân");
    // Cân thưa ở ĐẦU cửa sổ (ngày 0 và 6), ăn 2100 ngày 0-6 và 3500 sau đó: 2100 + 0.3·7700/6 = 2485; ở CUỐI (ngày 14 và 20), ăn 1000 trước và 2100 sau: cũng 2485
    var xStart = ExpenditureCalculator.ComputeRaw(Win(21, i => i <= 6 ? 2100m : 3500m, i => W(i, (0, 80.0m), (6, 79.7m))), 7700);
    var xEnd = ExpenditureCalculator.ComputeRaw(Win(21, i => i >= 14 ? 2100m : 1000m, i => W(i, (14, 80.0m), (20, 79.7m))), 7700);
    Check(xStart?.TdeeRaw == 2485m && xEnd?.TdeeRaw == 2485m, "EXP-001: cân thưa ở đầu hoặc cuối cửa sổ: chỉ ăn trong khoảng cân được tính (2485 cả hai)");
    // Ngày thiếu log trong khoảng: chỉ 3 ngày có log (2000/2200/2400) trong khoảng ngày 0-10, ngày 15 ăn 5000 nằm ngoài: avg 2200, raw = 2200 + 0.5·7700/10 = 2585
    var xMiss = ExpenditureCalculator.ComputeRaw(Win(21, i => i switch { 2 => 2000m, 4 => 2200m, 6 => 2400m, 15 => 5000m, _ => null }, i => W(i, (0, 80.0m), (10, 79.5m))), 7700);
    Check(xMiss is { TdeeRaw: 2585m, AvgKcalIn: 2200m, LoggedDays: 3 }, "EXP-001: ngày thiếu log trong khoảng không bị coi là 0 (trung bình 3 ngày có log), ngày log ngoài khoảng bị bỏ");
    // Không có log hợp lệ trong khoảng cân ⇒ null (rơi về Holding, giữ TDEE cũ, không tạo TDEE bất thường): log chỉ ở ngày 0-3, cân ngày 5 và 10
    Check(ExpenditureCalculator.ComputeRaw(Win(21, i => i <= 3 ? 2000m : null, i => W(i, (5, 80m), (10, 79.8m))), 7700) is null, "EXP-001: không có ngày log nào trong khoảng cân ⇒ null (Holding), không tính TDEE từ log ngoài khoảng");
    // 0 kcal hợp lệ (nhịn ăn) ≠ chưa log: ngày 2 = 0 kcal ⇒ avg 1600 ⇒ 1600 + 0.1·7700/4 = 1792.5; ngày 2 chưa log (null) ⇒ avg 2000 ⇒ 2192.5
    var xFast = ExpenditureCalculator.ComputeRaw(Win(5, i => i == 2 ? 0m : 2000m, i => W(i, (0, 80.0m), (4, 79.9m))), 7700);
    var xUnlog = ExpenditureCalculator.ComputeRaw(Win(5, i => i == 2 ? null : 2000m, i => W(i, (0, 80.0m), (4, 79.9m))), 7700);
    Check(xFast?.TdeeRaw == 1792.5m && xFast.LoggedDays == 5 && xUnlog?.TdeeRaw == 2192.5m && xUnlog.LoggedDays == 4, "EXP-001: ngày 0 kcal hợp lệ (nhịn ăn) được tính vào trung bình, ngày chưa log thì không");
    // Dấu: tăng cân (70.0→70.7 trong 14 ngày, ăn 3000) = 3000 − 385 = 2615; duy trì (trend phẳng, ăn 2300) = 2300
    Check(ExpenditureCalculator.ComputeRaw(Win(15, i => 3000m, i => W(i, (0, 70.0m), (14, 70.7m))), 7700)?.TdeeRaw == 2615m
          && ExpenditureCalculator.ComputeRaw(Win(15, i => 2300m, i => W(i, (0, 80.0m), (14, 80.0m))), 7700)?.TdeeRaw == 2300m, "EXP-001: tăng cân 2615 (ăn − thặng dư), duy trì = lượng ăn 2300");
    // Biên bao gồm hai đầu: ngày 2 và 10 ngoài khoảng [3,9] (ăn 9999) bị loại; ngày 3, 5, 9 (2000) được tính: 2000 + 0.6·7700/6 = 2770
    var xBound = ExpenditureCalculator.ComputeRaw(Win(14, i => i switch { 2 => 9999m, 3 => 2000m, 5 => 2000m, 9 => 2000m, 10 => 9999m, _ => null }, i => W(i, (3, 80.0m), (9, 79.4m))), 7700);
    Check(xBound is { TdeeRaw: 2770m, LoggedDays: 3 }, "EXP-001: khoảng [lần cân đầu, lần cân cuối] bao gồm hai đầu, ngày ngay trước/sau bị loại");
    // Khoảng ngắn (1 ngày): giữ hành vi cũ (không thêm quy tắc mới, cần quyết định) nhưng làm mượt β=0.1 giới hạn bước TDEE: raw 2770 ⇒ 2400 → 2437
    var xShort = ExpenditureCalculator.ComputeRaw(Win(21, i => 2000m, i => W(i, (19, 79.9m), (20, 79.8m))), 7700);
    Check(xShort?.TdeeRaw == 2770m && ExpenditureCalculator.Smooth(2400m, xShort.TdeeRaw, 0.1m) == 2437m && ExpenditureCalculator.Smooth(2400m, 2577.5m, 0.1m) == 2417.75m, "EXP-001: khoảng cân 1 ngày cho 2770 (nhiễu, đã ghi nhận) nhưng EMA β=0.1 giới hạn bước (2437); liên tục từ TDEE đã học 2400 → 2417.75");
    // Một lần cân ⇒ null
    Check(ExpenditureCalculator.ComputeRaw(Win(21, i => 2000m, i => W(i, (10, 80m))), 7700) is null, "EXP-001: chỉ 1 lần cân ⇒ null");
    // Trend nội suy thật: cân 80 (ngày 0) và 79 (ngày 10) qua WeightTrendCalculator (EMA N=20, nội suy ngày trống): trend ngày 10 = 79.60 (Python decimal) ⇒ raw = 2200 + 0.40·7700/10 = 2508
    var interp = WeightTrendCalculator.Compute([(e0, 80m), (e0.AddDays(10), 79m)], 20);
    var xInterp = ExpenditureCalculator.ComputeRaw(Win(11, i => 2200m, i => interp.TryGetValue(e0.AddDays(i), out var t) ? t : null), 7700);
    Check(interp[e0.AddDays(10)] == 79.60m && xInterp?.TdeeRaw == 2508m, "EXP-001: trend nội suy/làm mượt thật: trend ngày 10 = 79.60, raw 2508 (vector Python độc lập)");
    // Phiên bản: v6 (đóng băng) / v7 (học từ ngày 1); v4/v5 cũ hơn nên bị ghi đè khi dựng lại; snapshot v6/v7 không bị job v4/v5 đè
    Check(AlgorithmVersions.IsNewer("6", "5") && AlgorithmVersions.IsNewer("7", "6") && !AlgorithmVersions.IsNewer("5", "6") && !AlgorithmVersions.IsNewer("4", "6"), "EXP-001: nhãn v6/v7 mới hơn v4/v5 (chỉ tính mới, không đè dòng cũ hơn)");
}

// ---- Job đêm theo giờ local (mỗi giờ, mỗi user đúng 1 lần/ngày local) + hysteresis nhãn ----
{
    int DueCount(string? tz, DateTimeOffset dayStartUtc) => Enumerable.Range(0, 24).Count(h => NightlyWindow.IsDue(tz, dayStartUtc.AddHours(h).AddMinutes(30)));
    var day0 = new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero);
    var tzs = new string?[] { "UTC", "Asia/Bangkok", "America/New_York", "Asia/Kolkata", "Asia/Kathmandu", "Pacific/Auckland", null, "Not/AZone" };
    Check(tzs.All(z => DueCount(z, day0) == 1), "Nightly window: mỗi múi giờ (UTC+7, UTC−5, +5:30, +5:45, +12, thiếu, không hợp lệ) đúng MỘT lần chạy/ngày trong 24 tick hàng giờ");
    Check(NightlyWindow.IsDue("Asia/Bangkok", new DateTimeOffset(2026, 6, 9, 17, 30, 0, TimeSpan.Zero)) && !NightlyWindow.IsDue("Asia/Bangkok", new DateTimeOffset(2026, 6, 9, 18, 30, 0, TimeSpan.Zero))
          && NightlyWindow.IsDue("America/New_York", new DateTimeOffset(2026, 6, 10, 4, 30, 0, TimeSpan.Zero)),
          "Nightly window: UTC+7 chạy lúc 17:30 UTC (00:30 local), UTC−5/−4 chạy đúng 00:30 local");
    var dstDays = new[] { ("America/New_York", new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero)), ("America/New_York", new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)), ("Europe/London", new DateTimeOffset(2026, 10, 25, 0, 0, 0, TimeSpan.Zero)) };
    Check(dstDays.All(x => DueCount(x.Item1, x.Item2) <= 2 && DueCount(x.Item1, x.Item2.AddDays(1)) <= 2 && DueCount(x.Item1, x.Item2) + DueCount(x.Item1, x.Item2.AddDays(1)) >= 1),
          "Nightly window: ngày DST (nhảy giờ/lùi giờ) không bỏ lỡ cả hai ngày liên tiếp và không chạy quá 2 lần/ngày UTC (dựng lại idempotent)");

    ExpenditureSeriesPoint Pt(int d, string s) => new(new DateOnly(2026, 1, 1).AddDays(d), 2400m, s, 7, false);
    string Lbl(params string[] s) => string.Concat(ExpenditureSeries.SmoothLabels(s.Select((x, i) => Pt(i, x)).ToList()).Select(p => p.Status[0]));
    Check(Lbl("Updating", "Holding", "Updating", "Holding", "Updating") == "UUUUU", "Hysteresis nhãn: Updating↔Holding nháy mỗi ngày KHÔNG đổi nhãn hiển thị");
    Check(Lbl("Updating", "Holding", "Holding", "Holding", "Updating", "Updating") == "UUHHHU", "Hysteresis nhãn: status mới giữ 2 ngày liên tiếp thì nhãn đổi (ở ngày thứ 2), về Updating cũng vậy");
    Check(Lbl("Calibrating", "Updating", "Holding") == "CUU" && Lbl("Calibrating", "Calibrating", "Updating") == "CCU", "Hysteresis nhãn: rời Calibrating đổi ngay (không trễ Updating đầu tiên)");
    Check(ExpenditureSeries.SmoothLabels([]).Count == 0 && ExpenditureSeries.SmoothLabels([Pt(0, "Holding")])[0].Tdee == 2400m, "Hysteresis nhãn: chuỗi rỗng/1 điểm an toàn, TDEE không bị đổi");
}

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
    public Task<bool> TryAcquireTransactionLockAsync(string resource, int timeoutMs, CancellationToken ct = default) => Task.FromResult(true);
    public void DiscardChanges() { }
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
