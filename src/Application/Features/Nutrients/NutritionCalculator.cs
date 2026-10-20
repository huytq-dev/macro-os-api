namespace Application;

/// <summary>
/// Chính sách làm tròn dinh dưỡng, một nơi duy nhất. Ba tầng:
/// (1) tính toán nội bộ: decimal, không làm tròn giữa chừng;
/// (2) snapshot lưu DB: <see cref="Macro"/> / <see cref="Micro"/> số lẻ, giữ nguyên để số liệu đã lưu không đổi;
/// (3) hiển thị: theo <c>NutrientDefinition.DisplayPrecision</c> ở lớp trình bày, hoặc phản hồi gọn (<see cref="CompactKcal"/>/<see cref="CompactMacro"/>).
/// Chế độ làm tròn là ToEven, đúng mặc định của <c>Math.Round</c> mà mọi snapshot hiện có đã dùng; đổi chế độ sẽ làm lệch số cũ.
/// </summary>
public static class NutritionRounding
{
    public const MidpointRounding Mode = MidpointRounding.ToEven;

    /// <summary>Cột snapshot của FoodLogEntry (kcal/P/C/F/fiber/sodium), tổng và khẩu phần recipe.</summary>
    public const int Macro = 2;
    /// <summary>FoodNutrient / FoodLogEntryNutrient và API chất theo món/entry.</summary>
    public const int Micro = 4;
    public const int Percent = 1;
    /// <summary>Gram nguyên liệu recipe.</summary>
    public const int Grams = 2;
    /// <summary>Phản hồi gọn: kcal số nguyên, macro 1 số lẻ (yêu thích, ước lượng AI).</summary>
    public const int CompactKcal = 0, CompactMacro = 1;

    public static decimal Round(decimal value, int decimals) => Math.Round(value, decimals, Mode);

    public static decimal? Round(decimal? value, int decimals) => value is { } v ? Round(v, decimals) : null;
}

/// <summary>Đơn vị khối lượng/thể tích đổi ra gram. Đơn vị thể tích cần tỉ trọng (g/ml).</summary>
public enum QuantityUnit { Gram, Kilogram, Ounce, Pound, Milliliter, Liter, FluidOunce }

/// <summary>Khẩu phần đã chọn cho một khối lượng; ServingId null = log theo gram (quantity = gram, unitLabel "g").</summary>
public sealed record ServingChoice(Guid? ServingId, decimal Quantity, string UnitLabel, decimal WeightGrams);

/// <summary>Macro + fiber + sodium của một entry (cột snapshot FoodLogEntry). Null = chưa có dữ liệu, khác 0.</summary>
public sealed record EntryMacros(decimal Calories, decimal Protein, decimal Carbs, decimal Fat, decimal? Fiber, decimal? Sodium);

/// <summary>Tổng ngày cộng từ snapshot đã lưu của các entry chưa xoá.</summary>
public sealed record DayTotals(decimal Calories, decimal Protein, decimal Carbs, decimal Fat, decimal Fiber);

/// <summary>Kết quả đối chiếu kcal với macro; không bao giờ tự sửa dữ liệu nguồn.</summary>
public sealed record EnergyCheck(bool IsConsistent, decimal ExpectedKcal, decimal DifferenceKcal);

/// <summary>
/// Công thức dinh dưỡng thuần (không DB, HTTP, EF, LLM) dùng chung cho Food Log, Recipe, AI resolve và Nutrition Overview.
/// "Thiếu" khác 0: hồ sơ chất không có khoá = chưa biết; giá trị 0 là đã biết bằng 0.
/// </summary>
public static class NutritionCalculator
{
    private const decimal OunceGrams = 28.349523125m, PoundGrams = 453.59237m, FluidOunceMl = 29.5735295625m;

    // --- Khối lượng và khẩu phần ---

    /// <summary>
    /// Gram thực ăn. <paramref name="servingWeightGrams"/> null = log theo gram (quantity là gram); có khẩu phần thì quantity × gram khẩu phần.
    /// Quantity ≤ 0 hoặc khẩu phần có gram ≤ 0 (dữ liệu hỏng) → 0.
    /// </summary>
    public static decimal Grams(decimal quantity, decimal? servingWeightGrams)
    {
        if (quantity <= 0) return 0m;
        return servingWeightGrams switch
        {
            null => quantity,
            > 0 => quantity * servingWeightGrams.Value,
            _ => 0m,
        };
    }

    /// <summary>Đổi lượng sang gram. Đơn vị thể tích mà chưa biết tỉ trọng (null hoặc ≤ 0) → null, không đoán 1 g/ml. Lượng ≤ 0 → 0.</summary>
    public static decimal? ToGrams(decimal amount, QuantityUnit unit, decimal? densityGPerMl = null)
    {
        if (amount <= 0) return 0m;
        decimal? Volume(decimal ml) => densityGPerMl is > 0 ? ml * densityGPerMl.Value : null;
        return unit switch
        {
            QuantityUnit.Gram => amount,
            QuantityUnit.Kilogram => amount * 1000m,
            QuantityUnit.Ounce => amount * OunceGrams,
            QuantityUnit.Pound => amount * PoundGrams,
            QuantityUnit.Milliliter => Volume(amount),
            QuantityUnit.Liter => Volume(amount * 1000m),
            QuantityUnit.FluidOunce => Volume(amount * FluidOunceMl),
            _ => null,
        };
    }

    /// <summary>
    /// Khẩu phần gần <paramref name="grams"/> nhất (theo tỉ lệ). Ra 0.25–4 khẩu phần thì log theo khẩu phần, ngoài khoảng hoặc món không có khẩu phần
    /// (gram ≤ 0 bị bỏ qua) thì log theo gram (servingId null, quantity = gram như entry log theo gram).
    /// </summary>
    public static ServingChoice ChooseServing(decimal grams, IReadOnlyList<(Guid Id, string Name, decimal Grams)> servings)
    {
        if (grams <= 0) return new ServingChoice(null, 0m, "g", 0m);

        var best = servings.Where(s => s.Grams > 0)
            .OrderBy(s => Math.Abs(Math.Log((double)(grams / s.Grams))))
            .Select(s => ((Guid Id, string Name, decimal Grams)?)s)
            .FirstOrDefault();
        if (best is { } s0)
        {
            var q = grams / s0.Grams;
            if (q is >= 0.25m and <= 4m)
            {
                var qty = NutritionRounding.Round(q, 2);
                return new ServingChoice(s0.Id, qty, s0.Name, NutritionRounding.Round(qty * s0.Grams, 1));
            }
        }
        return new ServingChoice(null, grams, "g", grams);
    }

    // --- Co giãn ---

    /// <summary>Hệ số của giá trị trên 100 g cho <paramref name="grams"/> gram (gram âm coi như 0).</summary>
    public static decimal GramFactor(decimal grams) => Math.Max(grams, 0m) / 100m;

    /// <summary>Lượng cho <paramref name="grams"/> gram từ giá trị trên 100 g, làm tròn theo <paramref name="decimals"/>.</summary>
    public static decimal Scale(decimal per100g, decimal grams, int decimals = NutritionRounding.Macro)
        => NutritionRounding.Round(per100g * GramFactor(grams), decimals);

    /// <summary>Lượng của một chất trong hồ sơ nhân với <paramref name="factor"/> (gram/100 với món, số khẩu phần với recipe); chất vắng mặt → null, khác 0.</summary>
    public static decimal? ScaleNutrient(IReadOnlyDictionary<string, NutrientValue>? profile, string code, decimal factor, int decimals = NutritionRounding.Macro)
        => profile is not null && profile.TryGetValue(code, out var v) ? NutritionRounding.Round(v.Amount * factor, decimals) : null;

    /// <summary>Nhân cả hồ sơ với <paramref name="factor"/>, giữ trạng thái Measured/Estimated và chất nào vắng mặt vẫn vắng mặt.</summary>
    public static Dictionary<string, NutrientValue> ScaleProfile(IReadOnlyDictionary<string, NutrientValue> profile, decimal factor, int decimals = NutritionRounding.Micro)
        => profile.ToDictionary(p => p.Key, p => new NutrientValue(NutritionRounding.Round(p.Value.Amount * factor, decimals), p.Value.Status));

    /// <summary>Đổi tỉ lệ một giá trị đã lưu (sửa số khẩu phần của entry): làm tròn lại từ giá trị đã làm tròn nên sai số có thể cộng dồn qua nhiều lần sửa.</summary>
    public static decimal Rescale(decimal value, decimal ratio, int decimals = NutritionRounding.Macro) => NutritionRounding.Round(value * ratio, decimals);

    public static decimal? Rescale(decimal? value, decimal ratio, int decimals = NutritionRounding.Macro) => value is { } v ? Rescale(v, ratio, decimals) : null;

    /// <summary>Snapshot macro của món trong <paramref name="grams"/> gram: 4 macro từ cột /100 g của món; fiber và sodium từ hồ sơ (vắng mặt → null).</summary>
    public static EntryMacros SnapshotMacros(decimal kcalPer100g, decimal proteinPer100g, decimal carbsPer100g, decimal fatPer100g,
        IReadOnlyDictionary<string, NutrientValue>? profile, decimal grams)
    {
        var k = GramFactor(grams);
        return new EntryMacros(
            Scale(kcalPer100g, grams), Scale(proteinPer100g, grams), Scale(carbsPer100g, grams), Scale(fatPer100g, grams),
            ScaleNutrient(profile, "fiber_g", k), ScaleNutrient(profile, "sodium_mg", k));
    }

    /// <summary>Snapshot macro từ hồ sơ trên 1 khẩu phần × số khẩu phần (recipe). Macro vắng mặt → 0, fiber/sodium vắng mặt → null.</summary>
    public static EntryMacros SnapshotFromProfile(IReadOnlyDictionary<string, NutrientValue> perServing, decimal servings)
        => new(
            ScaleNutrient(perServing, "energy_kcal", servings) ?? 0m, ScaleNutrient(perServing, "protein_g", servings) ?? 0m,
            ScaleNutrient(perServing, "carbs_g", servings) ?? 0m, ScaleNutrient(perServing, "fat_g", servings) ?? 0m,
            ScaleNutrient(perServing, "fiber_g", servings), ScaleNutrient(perServing, "sodium_mg", servings));

    /// <summary>
    /// Lượng từng chất của một dòng log theo đúng cách snapshot được lưu: chất nằm ở cột entry (kcal/P/C/F/fiber/sodium) làm tròn <see cref="NutritionRounding.Macro"/>,
    /// chất còn lại (FoodLogEntryNutrient) làm tròn <see cref="NutritionRounding.Micro"/>. Dùng để xem trước dinh dưỡng của món chưa log mà vẫn khớp số khi log thật.
    /// </summary>
    public static Dictionary<string, decimal> SnapshotAmounts(IReadOnlyDictionary<string, NutrientValue> profile, decimal factor)
        => profile.ToDictionary(
            p => p.Key,
            p => NutritionRounding.Round(p.Value.Amount * factor, NutrientSnapshot.ColumnCodes.Contains(p.Key) ? NutritionRounding.Macro : NutritionRounding.Micro));

    /// <summary>Cộng từng chất qua nhiều dòng (không làm tròn lại); chất không dòng nào có thì vắng mặt.</summary>
    public static Dictionary<string, decimal> SumAmounts(IEnumerable<IReadOnlyDictionary<string, decimal>> parts)
    {
        var sums = new Dictionary<string, decimal>();
        foreach (var part in parts)
            foreach (var (code, amount) in part)
                sums[code] = sums.GetValueOrDefault(code) + amount;
        return sums;
    }

    // --- Cộng gộp ---

    /// <summary>
    /// Tổng chất của cả recipe từ (hồ sơ /100 g, gram), chưa làm tròn. Chất mà không phải mọi nguyên liệu đều có, hoặc có nguyên liệu không phải Measured
    /// → Estimated (tổng thiếu phần chưa biết). Chất có giá trị 0 vẫn tính là có.
    /// </summary>
    public static Dictionary<string, NutrientValue> SumProfiles(IReadOnlyCollection<(IReadOnlyDictionary<string, NutrientValue> Profile, decimal Grams)> parts)
    {
        var sums = new Dictionary<string, (decimal Amount, int Count, bool AllMeasured)>();
        foreach (var (profile, grams) in parts)
            foreach (var (code, v) in profile)
            {
                var cur = sums.TryGetValue(code, out var c) ? c : (Amount: 0m, Count: 0, AllMeasured: true);
                sums[code] = (cur.Amount + v.Amount * GramFactor(grams), cur.Count + 1, cur.AllMeasured && v.Status == NutrientAmountStatus.Measured);
            }
        return sums.ToDictionary(
            s => s.Key,
            s => new NutrientValue(s.Value.Amount, s.Value.Count == parts.Count && s.Value.AllMeasured ? NutrientAmountStatus.Measured : NutrientAmountStatus.Estimated));
    }

    /// <summary>Tổng ngày cộng thẳng từ snapshot đã lưu của entry chưa xoá (không làm tròn lại, để tổng khớp những gì user thấy ở từng entry).</summary>
    public static DayTotals SumEntries(IEnumerable<FoodLogEntry> entries)
    {
        var live = entries.Where(e => !e.IsDeleted).ToList();
        return new DayTotals(live.Sum(e => e.Calories), live.Sum(e => e.Protein), live.Sum(e => e.Carbs), live.Sum(e => e.Fat), live.Sum(e => e.Fiber ?? 0m));
    }

    /// <summary>Chia tổng hồ sơ cho số khẩu phần (tối thiểu 1) → hồ sơ trên 1 khẩu phần; giữ trạng thái, chưa làm tròn.</summary>
    public static Dictionary<string, NutrientValue> PerServing(IReadOnlyDictionary<string, NutrientValue> total, int servings)
        => total.ToDictionary(t => t.Key, t => new NutrientValue(t.Value.Amount / Math.Max(servings, 1), t.Value.Status));

    /// <summary>Chia một tổng cho số khẩu phần (tối thiểu 1) rồi làm tròn.</summary>
    public static decimal PerServing(decimal total, int servings, int decimals = NutritionRounding.Macro)
        => NutritionRounding.Round(total / Math.Max(servings, 1), decimals);

    /// <summary>Trung bình mỗi ngày đã log của một tổng cả kỳ; không có ngày nào (hoặc chưa có tổng) → chia 1 / coi tổng là 0, không chia cho 0.</summary>
    public static decimal AveragePerLoggedDay(decimal? sum, int loggedDays, int decimals = NutritionRounding.Macro)
        => NutritionRounding.Round((sum ?? 0m) / Math.Max(loggedDays, 1), decimals);

    /// <summary>Như <see cref="AveragePerLoggedDay"/> nhưng chưa có dữ liệu chất (tổng null) → null, không biến thành 0 (FE hiện "—").</summary>
    public static decimal? AveragePerLoggedDayOrNull(decimal? sum, int loggedDays, int decimals = NutritionRounding.Macro)
        => sum is { } s ? NutritionRounding.Round(s / Math.Max(loggedDays, 1), decimals) : null;

    // --- Giá trị dẫn xuất ---

    /// <summary>Năng lượng suy từ macro: 4P + 4C + 9F, cộng 7 kcal/g cồn nếu có dữ liệu.</summary>
    public static decimal AtwaterKcal(decimal protein, decimal carbs, decimal fat, decimal alcoholGrams = 0m)
        => 4m * protein + 4m * carbs + 9m * fat + 7m * alcoholGrams;

    /// <summary>
    /// Đối chiếu kcal đã có với kcal suy từ macro: lệch quá max(<paramref name="minToleranceKcal"/>, <paramref name="tolerance"/> × kỳ vọng) là không nhất quán.
    /// Macro đều 0 thì không có cơ sở đánh giá → nhất quán. Chỉ trả kết quả, KHÔNG sửa kcal của USDA hay nguồn khác.
    /// </summary>
    public static EnergyCheck ValidateEnergy(decimal kcal, decimal protein, decimal carbs, decimal fat, decimal alcoholGrams = 0m,
        decimal tolerance = 0.20m, decimal minToleranceKcal = 30m)
    {
        var expected = AtwaterKcal(protein, carbs, fat, alcoholGrams);
        var diff = kcal - expected;
        var consistent = expected <= 0 || Math.Abs(diff) <= Math.Max(minToleranceKcal, tolerance * expected);
        return new EnergyCheck(consistent, expected, diff);
    }

    /// <summary>Kcal dùng cho ước lượng AI: giữ kcal nếu khớp macro, lệch quá ngưỡng thì lấy theo macro. Chỉ gọi ở biên AI, không dùng cho dữ liệu món.</summary>
    public static decimal ReconcileKcal(decimal kcal, decimal protein, decimal carbs, decimal fat, decimal alcoholGrams = 0m)
    {
        var check = ValidateEnergy(kcal, protein, carbs, fat, alcoholGrams);
        return check.IsConsistent ? kcal : NutritionRounding.Round(check.ExpectedKcal, NutritionRounding.CompactKcal);
    }

    /// <summary>Carbs trừ fiber, không âm; chưa biết fiber → null (không đoán fiber = 0 làm net carbs bị thổi phồng).</summary>
    public static decimal? NetCarbs(decimal carbs, decimal? fiber) => fiber is { } f ? Math.Max(carbs - f, 0m) : null;

    // --- So với target ---

    /// <summary>% so với target, 1 số lẻ; chưa biết lượng hoặc target ≤ 0 / chưa đặt → null (không chia cho 0).</summary>
    public static decimal? PercentOfTarget(decimal? amount, decimal? target)
        => amount is { } a && target is > 0 ? NutritionRounding.Round(a / target.Value * 100m, NutritionRounding.Percent) : null;

    /// <summary>Còn lại so với target (âm = vượt); chưa đặt target → null.</summary>
    public static decimal? Remaining(decimal? target, decimal consumed) => target is { } t ? t - consumed : null;

    /// <summary>Nằm trong [floor, ceiling]; mốc nào chưa đặt thì bỏ qua. Lượng chưa biết do nơi gọi xử lý (không có lượng thì không đánh giá).</summary>
    public static bool WithinRange(decimal amount, decimal? floor, decimal? ceiling)
        => (floor is null || amount >= floor) && (ceiling is null || amount <= ceiling);
}
