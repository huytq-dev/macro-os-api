using Contract;
// Self-check cho MacroTargetCalculator (ALG-01). Chạy: dotnet run --project tests/AlgorithmChecks
using Application;
using Domain;

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
var rice = new FoodItem { Id = Guid.NewGuid(), Name = "Rice", CaloriesPer100g = 130, ProteinPer100g = 2.7m, CarbsPer100g = 28, FatPer100g = 0.3m, FiberPer100g = 0.4m };
var bowl = new FoodServing { Id = Guid.NewGuid(), FoodItemId = rice.Id, ServingName = "bowl", WeightInGrams = 150 };
var e1 = new FoodLogEntry(); FoodLogSupport.Snapshot(e1, rice, FoodLogSupport.Grams(2, bowl));   // 300 g
var e2 = new FoodLogEntry(); FoodLogSupport.Snapshot(e2, rice, FoodLogSupport.Grams(50, null));  // 50 g
Check(e1.Calories == 390m && e1.Protein == 8.1m && e1.Fiber == 1.2m, $"Snapshot 2 bowl = {e1.Calories} kcal");
Check(e2.Calories == 65m && e2.Carbs == 14m, "Snapshot 50 g không serving");
var log = new FoodLog { Entries = [e1, e2, new FoodLogEntry { Calories = 999, IsDeleted = true }] };
FoodLogSupport.RecalcTotals(log);
Check(log.TotalCalories == 455m && log.TotalFiber == 1.4m, $"Tổng ngày bỏ entry đã xoá = {log.TotalCalories}");

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
Check(FoodLogSupport.IsLogged(FoodLogStatus.Unmarked, true), "Có món, chưa đánh dấu → đã log");
Check(!FoodLogSupport.IsLogged(FoodLogStatus.Unmarked, false), "Dòng FoodLog rỗng → chưa log (xoá entry cuối)");
Check(!FoodLogSupport.IsLogged(FoodLogStatus.Complete, false), "Complete nhưng không có món → chưa log");
Check(FoodLogSupport.IsLogged(FoodLogStatus.Fasting, false), "Fasting ngày trống → đã log (0 kcal thật)");
Check(!FoodLogSupport.IsLogged(FoodLogStatus.Partial, true), "Partial có món → không vào thuật toán");
Check(new FoodLogSupport.LoggedDay(g0, FoodLogStatus.Fasting, true, 0, 0, 0, 0, 0, 0).AlgorithmKcal == 0m, "Fasting → kcal thuật toán = 0");
Check(new FoodLogSupport.LoggedDay(g0, FoodLogStatus.Partial, false, 300, 0, 0, 0, 0, 0).AlgorithmKcal is null, "Partial → kcal thuật toán null dù số hiển thị 300");

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
    "Standard", true, false, true, "Edit", true, false, false, false, "PlateOnly",
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
var banner = Mapster.TypeAdapter.Adapt<FoodLogSettingsModel>(new FoodLogSetting { LoggerBannerShowCalories = false, LoggerBannerShowProtein = true, LoggerBannerDisplay = BannerDisplay.RemainingAfterPlate });
Check(banner.LoggerBannerDisplay == "RemainingAfterPlate" && banner.LoggerBannerShowProtein && !banner.LoggerBannerShowCalories && !banner.LoggerBannerShowFat && !banner.LoggerBannerShowCarbs, "Cài đặt: 5 trường banner → model");
Check(!settingsValidator.Validate(Settings() with { LoggerTileDensity = (TileDensity)9 }).IsValid && !settingsValidator.Validate(Settings() with { LoggerHighlight = (SearchRowHighlight)9 }).IsValid, "Cài đặt: enum Logger tiles ngoài miền bị từ chối");
var logger = Mapster.TypeAdapter.Adapt<FoodLogSettingsModel>(new FoodLogSetting { LoggerTileDensity = TileDensity.Comfort, LoggerHighlight = SearchRowHighlight.Energy, LoggerShowWeight = true, LoggerShowMacros = false });
Check(logger.LoggerTileDensity == "Comfort" && logger.LoggerHighlight == "Energy" && logger.LoggerShowWeight && !logger.LoggerShowMacros && logger.LoggerShowServing, "Cài đặt: 5 trường Logger tiles → model");
Check(FoodEmoji.For(FoodCategory.Fruits) == "🍎" && FoodEmoji.For(FoodCategory.Other) is null && FoodEmoji.For(null) is null, "Emoji: nhóm → emoji, Other/không có món → null");

var pointModel = Mapster.TypeAdapter.Adapt<ExpenditurePointModel>(new ExpenditureSnapshot { SnapshotDate = g0, EstimatedTdeeKcal = 2429m, Status = ExpenditureStatus.Holding, WindowDays = 21 });
Check(pointModel.EstimatedTdeeKcal == 2429m && pointModel.Status == "Holding" && pointModel.FluxLowKcal is null, "Mapster ExpenditurePointModel (flux gán sau ở handler)");

Console.WriteLine(Environment.ExitCode == 0 ? "ALL PASS (incl. ALG-02, ALG-03, ALG-04, MOS-005, Dashboard S9, Expenditure flux)" : "SOME FAIL");
