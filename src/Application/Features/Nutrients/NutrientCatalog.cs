namespace Application;

public static class NutrientCatalog
{
    /// <summary>Target của bốn code này đến từ program (MacroProgramWeek), không cho user ghi đè ở UserNutrientTarget.</summary>
    public static readonly string[] MacroCodes = ["energy_kcal", "protein_g", "carbs_g", "fat_g"];

    public const string NetCarbsCode = "carbs_net_g";

    /// <summary>Chất chỉ dùng làm nguồn quy đổi/chỉ có nghĩa với dạng bổ sung hoặc không phải chất dinh dưỡng: không trả ra UI (vẫn lưu ở món để suy chất tổng).</summary>
    public static readonly string[] HiddenCodes = ["ash_g", "vitamin_a_iu", "vitamin_d_iu", "vitamin_e_added_mg", "vitamin_b12_added_mcg"];

    /// <summary>Nguồn của floor/target/ceiling hiệu lực (<see cref="NutrientModel.TargetBasis"/>).</summary>
    public static class Basis
    {
        public const string User = "user", Program = "program", Dri = "dri", Reference = "reference", Default = "default", None = "none";
    }

    /// <summary>Danh sách nutrient kèm target hiệu lực + pin của user; withAmounts = tính lượng đã ăn của <paramref name="date"/>.</summary>
    public static async Task<List<NutrientModel>> BuildAsync(IUnitOfWork uow, Guid userId, DateOnly date, bool withAmounts, CancellationToken ct, bool includeExtended = false)
    {
        // Mặc định chỉ chất core (bộ 56 khoá FE); includeExtended = thêm chất chi tiết của USDA (acid béo, đường, tocopherol…)
        var defs = await uow.Repository<NutrientDefinition>().Query()
            .Where(d => (includeExtended || d.IsCore) && !HiddenCodes.Contains(d.Code))
            .OrderBy(d => d.DisplayOrder).ToListAsync(ct);
        var pins = (await uow.Repository<UserNutrientTarget>().Query().Where(t => t.UserId == userId && t.PinOrder != null)
            .Select(t => new { t.NutrientDefinitionId, t.PinOrder }).ToListAsync(ct)).ToDictionary(t => t.NutrientDefinitionId, t => t.PinOrder);
        // Ghi đè tuỳ chỉnh THEO NGÀY: lần đổi gần nhất có hiệu lực ≤ date (lịch sử, xem NutrientGoalHistory)
        var history = await NutrientGoalHistory.LoadAsync(uow, userId, date, ct);
        var macro = await FoodLogSupport.TargetForDateAsync(uow, userId, date, ct);
        var amounts = withAmounts ? await AmountsAsync(uow, userId, date, date, defs, ct) : [];
        // Calories/P/F/C của ngày theo Effective Daily Nutrition: ngày có manual override thì lượng = đúng giá trị manual (trường chưa nhập = null, không lấy từ món, không 0); vi chất, fiber, sodium, nước vẫn từ món
        var effective = withAmounts ? (await FoodLogSupport.LoadDaysAsync(uow, userId, date, date, ct, includeFasting: true)).GetValueOrDefault(date) : null;
        var overridden = effective is { HasOverride: true };
        if (overridden)
            (amounts["energy_kcal"], amounts["protein_g"], amounts["carbs_g"], amounts["fat_g"]) = (effective!.Calories, effective.Protein, effective.Carbs, effective.Fat);

        // Hồ sơ cho DRI: thiếu ngày sinh hoặc không có hồ sơ → mức tham chiếu người lớn (xem DriTable)
        var profile = await uow.Repository<UserProfile>().Query().Where(p => p.UserId == userId).Select(p => new { p.DateOfBirth, p.Gender }).FirstOrDefaultAsync(ct);
        var gender = profile?.Gender ?? Gender.Other;
        var age = DriTable.AgeOn(profile?.DateOfBirth, date);
        decimal? kcal = macro?.CaloriesTarget;

        var items = new List<NutrientModel>(defs.Count + 1);
        foreach (var d in defs)
        {
            // Mức hiệu lực (NutrientGoals.Resolve): seed → DRI chỉ ở trường DRI có định nghĩa → user NGUYÊN BỘ (không trộn từng trường). Calories/P/F/C = target của program, không floor/ceiling
            var userGoal = history.TryGetValue(d.Id, out var changes) ? NutrientGoalHistory.On(changes, date) : null;
            var goal = NutrientGoals.Resolve(d.ReferenceMin, d.ReferenceTarget, d.ReferenceMax, DriTable.Resolve(d.Code, gender, age, kcal, out var reference), reference, userGoal);
            var custom = userGoal is { IsEmpty: false };

            if (macro is not null && MacroCodes.Contains(d.Code))
            {
                goal = NutrientGoals.Make(null, d.Code switch { "energy_kcal" => macro.CaloriesTarget, "protein_g" => macro.ProteinTarget, "carbs_g" => macro.CarbsTarget, _ => macro.FatTarget }, null, Basis.Program);
                custom = false;
            }
            var noTarget = goal.Kind == NutrientGoals.None;
            if (noTarget) goal = goal with { Basis = Basis.None };

            amounts.TryGetValue(d.Code, out var amount);
            items.Add(new NutrientModel(d.Code, d.Name, d.Unit, d.Group.ToString(), d.DisplayOrder, d.DisplayPrecision, amount, goal.Floor, goal.Target, goal.Ceiling, custom, pins.GetValueOrDefault(d.Id), d.IsCore, noTarget, goal.Basis, goal.Kind));

            if (withAmounts && d.Code == "fiber_g")
            {
                // Chưa có dữ liệu fiber thì net carbs cũng chưa biết (null), không coi fiber = 0; ngày có override: carbs là manual còn fiber chỉ từ món ⇒ không ghép khác nguồn, để null
                var net = overridden ? null : NutritionCalculator.NetCarbs(amounts.GetValueOrDefault("carbs_g") ?? 0, amount);
                items.Add(new NutrientModel(NetCarbsCode, "Net (Non-fiber)", "g", nameof(NutrientGroup.Carb), d.DisplayOrder + 5, 1, net, null, null, null, false, null, true, true, Basis.None, NutrientGoals.None));
            }
        }
        return items;
    }

    /// <summary>Mức MẶC ĐỊNH (seed + DRI theo hồ sơ và kcal của program tại <paramref name="date"/>, không ghi đè của user) của một nutrient không phải Calories/P/F/C.</summary>
    public static async Task<NutrientGoal> DefaultGoalAsync(IUnitOfWork uow, Guid userId, NutrientDefinition def, DateOnly date, CancellationToken ct)
    {
        var (gender, dob, kcal) = await DriInputsAsync(uow, userId, date, ct);
        return NutrientGoals.Resolve(def.ReferenceMin, def.ReferenceTarget, def.ReferenceMax, DriTable.Resolve(def.Code, gender, DriTable.AgeOn(dob, date), kcal, out var reference), reference, null);
    }

    /// <summary>
    /// Mục tiêu TRUNG BÌNH theo các ngày của một kỳ (overview kỳ dài) cho nutrient có lịch sử ghi đè: mỗi ngày dùng mức hiệu lực CỦA NGÀY ĐÓ, rồi lấy trung bình từng trường qua các ngày có trường đó.
    /// Chỉ trả nutrient có lịch sử (còn lại dùng mức tại <paramref name="to"/> như catalog). <paramref name="days"/> rỗng → chỉ ngày <paramref name="to"/>.
    /// </summary>
    public static async Task<Dictionary<string, NutrientGoal>> AveragedGoalsAsync(IUnitOfWork uow, Guid userId, IReadOnlyCollection<DateOnly> days, DateOnly to, CancellationToken ct)
    {
        var history = await NutrientGoalHistory.LoadAsync(uow, userId, to, ct);
        var result = new Dictionary<string, NutrientGoal>();
        if (history.Count == 0) return result;

        var ids = history.Keys.ToList();
        var defs = await uow.Repository<NutrientDefinition>().Query().Where(d => ids.Contains(d.Id)).ToListAsync(ct);
        var (gender, dob, kcal) = await DriInputsAsync(uow, userId, to, ct);
        var sample = days.Count > 0 ? days : [to];
        foreach (var d in defs.Where(d => !MacroCodes.Contains(d.Code)))
        {
            var dri = DriTable.Resolve(d.Code, gender, DriTable.AgeOn(dob, to), kcal, out var reference);
            var daily = sample.Select(day => NutrientGoals.Resolve(d.ReferenceMin, d.ReferenceTarget, d.ReferenceMax, dri, reference, NutrientGoalHistory.On(history[d.Id], day))).ToList();
            decimal? Avg(Func<NutrientGoal, decimal?> f) { var v = daily.Select(f).Where(x => x.HasValue).Select(x => x!.Value).ToList(); return v.Count == 0 ? null : Math.Round(v.Average(), 4); }
            var last = daily[^1];
            result[d.Code] = NutrientGoals.Make(Avg(g => g.Floor), Avg(g => g.Target), Avg(g => g.Ceiling), last.Basis);
        }
        return result;
    }

    private static async Task<(Gender Gender, DateOnly? DateOfBirth, decimal? Kcal)> DriInputsAsync(IUnitOfWork uow, Guid userId, DateOnly date, CancellationToken ct)
    {
        var profile = await uow.Repository<UserProfile>().Query().Where(p => p.UserId == userId).Select(p => new { p.DateOfBirth, p.Gender }).FirstOrDefaultAsync(ct);
        var macro = await FoodLogSupport.TargetForDateAsync(uow, userId, date, ct);
        return (profile?.Gender ?? Gender.Other, profile?.DateOfBirth, macro?.CaloriesTarget);
    }

    /// <summary>Tổng lượng từng chất (theo mã) trong khoảng ngày [from, to]; null = chưa có dữ liệu chất đó. Overview chia cho số ngày đã log để ra trung bình.</summary>
    public static async Task<Dictionary<string, decimal?>> SumsAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var defs = await uow.Repository<NutrientDefinition>().Query().ToListAsync(ct);
        return await AmountsAsync(uow, userId, from, to, defs, ct);
    }

    private static async Task<Dictionary<string, decimal?>> AmountsAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, List<NutrientDefinition> defs, CancellationToken ct)
    {
        // Entry đã xoá (soft) bị query filter loại; macro + fiber + sodium đọc từ cột snapshot của entry
        var entries = await uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodLog.UserId == userId && e.FoodLog.LogDate >= from && e.FoodLog.LogDate <= to)
            .Select(e => new { e.Calories, e.Protein, e.Carbs, e.Fat, e.Fiber, e.Sodium })
            .ToListAsync(ct);

        var sums = await uow.Repository<FoodLogEntryNutrient>().Query()
            .Where(n => n.FoodLogEntry.FoodLog.UserId == userId && n.FoodLogEntry.FoodLog.LogDate >= from && n.FoodLogEntry.FoodLog.LogDate <= to && !n.FoodLogEntry.IsDeleted)
            .GroupBy(n => n.NutrientDefinitionId)
            .Select(g => new { Id = g.Key, Sum = g.Sum(n => n.Amount) })
            .ToListAsync(ct);
        var byId = sums.ToDictionary(s => s.Id, s => s.Sum);

        var water = await uow.Repository<FoodLog>().Query()
            .Where(l => l.UserId == userId && l.LogDate >= from && l.LogDate <= to)
            .SumAsync(l => l.TotalWater, ct);

        var result = new Dictionary<string, decimal?>
        {
            ["energy_kcal"] = entries.Sum(e => e.Calories),
            ["protein_g"] = entries.Sum(e => e.Protein),
            ["carbs_g"] = entries.Sum(e => e.Carbs),
            ["fat_g"] = entries.Sum(e => e.Fat),
            // fiber/sodium: ngày chưa log (không entry nào) = 0 như macro (hợp đồng "luôn là số"); ngày có món nhưng không món nào có số liệu = null (khác 0, Net carbs cũng chưa biết)
            ["fiber_g"] = entries.Count == 0 ? 0 : entries.Any(e => e.Fiber != null) ? entries.Sum(e => e.Fiber ?? 0) : null,
            ["sodium_mg"] = entries.Count == 0 ? 0 : entries.Any(e => e.Sodium != null) ? entries.Sum(e => e.Sodium ?? 0) : null,
        };
        // null = chưa có dữ liệu nutrient này trong ngày (khác 0)
        foreach (var d in defs.Where(d => !NutrientSnapshot.ColumnCodes.Contains(d.Code)))
            result[d.Code] = byId.GetValueOrDefault(d.Id);
        // Nước uống (WaterLog, ml ≈ g) cộng vào water_g
        if (water > 0) result["water_g"] = (result.GetValueOrDefault("water_g") ?? 0) + water;

        return result;
    }
}
