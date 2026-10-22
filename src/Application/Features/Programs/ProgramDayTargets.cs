namespace Application;

/// <summary>Chia lại ngân sách tuần cho 7 dòng MacroProgramDayTarget khi target tuần đổi (check-in, đổi goal, nhập TDEE). Hàm thuần.</summary>
public static class ProgramDayTargets
{
    // Sai lệch ≤ ngưỡng này giữa tổng 7 ngày và 7 × target tuần chỉ là sai số làm tròn của trung bình, không chia lại
    public const int RoundingTolerance = 3;

    public static List<MacroProgramDayTarget> FromInputs(Guid programId, IEnumerable<DayTargetInput> days)
        => days.Select(d => new MacroProgramDayTarget
        {
            Id = Guid.NewGuid(),
            MacroProgramId = programId,
            DayOfWeek = d.DayOfWeek,
            IsFastingDay = d.IsFastingDay,
            IsLocked = d.IsLocked,
            CaloriesTarget = d.IsFastingDay ? 0 : d.Calories,
            ProteinTarget = d.IsFastingDay ? 0 : d.ProteinG,
            CarbsTarget = d.IsFastingDay ? 0 : d.CarbsG,
            FatTarget = d.IsFastingDay ? 0 : d.FatG,
        }).ToList();

    /// <summary>Trung bình 7 ngày: là target "tuần" (kcal làm tròn, macro 1 số lẻ) để các chỗ chỉ biết target tuần vẫn đúng.</summary>
    public static MacroTargets Average(IEnumerable<MacroProgramDayTarget> rows, MacroProgramWeek? fallback = null)
    {
        decimal Kcal(MacroProgramDayTarget d) => d.IsFastingDay ? 0 : d.CaloriesTarget ?? fallback?.CaloriesTarget ?? 0;
        decimal P(MacroProgramDayTarget d) => d.IsFastingDay ? 0 : d.ProteinTarget ?? fallback?.ProteinTarget ?? 0;
        decimal C(MacroProgramDayTarget d) => d.IsFastingDay ? 0 : d.CarbsTarget ?? fallback?.CarbsTarget ?? 0;
        decimal F(MacroProgramDayTarget d) => d.IsFastingDay ? 0 : d.FatTarget ?? fallback?.FatTarget ?? 0;
        return new MacroTargets(
            (int)Math.Round(rows.Average(Kcal), MidpointRounding.ToEven),
            Math.Round(rows.Average(P), 1), Math.Round(rows.Average(C), 1), Math.Round(rows.Average(F), 1));
    }

    /// <summary>
    /// Ngân sách mới = 7 × week.CaloriesTarget. Ngày khoá/nhịn ăn giữ nguyên; các ngày còn lại co giãn theo tỉ lệ để tổng đúng ngân sách
    /// (chênh làm tròn dồn vào ngày lớn nhất). Protein giữ nguyên (ADR-005) trừ khi kcal ngày không đủ chứa; carb:fat giữ tỉ lệ cũ của phần kcal còn lại.
    /// Cuối cùng target tuần = trung bình thật của 7 ngày.
    /// </summary>
    public static void Rebalance(MacroProgram program, MacroProgramWeek week, bool validate = true)
    {
        var rows = program.DayTargets.ToList();
        if (rows.Count == 0) return;

        // Kế hoạch tính TRƯỚC, kiểm khả thi, rồi mới ghi: không bao giờ ghi ngày không hợp lệ và không bao giờ tự hạ protein/fat (INVALID_CALORIE_TARGET | MACRO_BUDGET_EXCEEDED | INFEASIBLE_PROGRAM).
        // validate = false chỉ cho Reject/Accept-không-đề-xuất (sao chép target đã chấp nhận): dữ liệu cũ không hợp lệ được báo riêng, không bị chặn/sửa ở đây
        var plan = Plan(rows, week);
        var assessment = TargetFeasibility.AssessRows(plan, week.CaloriesTarget * 7, ProgramWeekBuilder.EffectiveFloor(program));
        if (validate) TargetFeasibility.EnsureFeasible(assessment);
        foreach (var d in plan.Where(x => x.Changed && assessment.IsFeasible))   // không validate + không khả thi: không ghi ngày nào bị chia lại sai (giữ nguyên dòng cũ)
        {
            d.Row.CaloriesTarget = d.Calories;
            d.Row.ProteinTarget = d.ProteinG;
            d.Row.CarbsTarget = d.CarbsG;
            d.Row.FatTarget = d.FatG;
        }

        var avg = Average(rows, week);
        week.CaloriesTarget = avg.Calories;
        week.ProteinTarget = avg.ProteinG;
        week.CarbsTarget = avg.CarbsG;
        week.FatTarget = avg.FatG;
    }

    /// <summary>Một ngày sau khi chia lại (chưa ghi). <c>Changed</c> = dòng bị co giãn; ngày khoá/nhịn ăn và ngày không cần chia lại giữ nguyên giá trị.</summary>
    public sealed record PlannedDay(MacroProgramDayTarget Row, int Calories, decimal ProteinG, decimal CarbsG, decimal FatG, bool Changed);

    /// <summary>
    /// Hàm thuần: 7 ngày sau khi chia lại ngân sách <c>7 × week.CaloriesTarget</c> (cùng thuật toán với <see cref="Rebalance"/>, không đổi dòng nào). Protein của ngày KHÔNG bao giờ bị hạ:
    /// phần kcal còn lại (kcal − 4·P) chia carbs:fat theo tỉ lệ cũ và có thể ÂM nếu kcal ngày không đủ chứa protein; khi đó <see cref="TargetFeasibility"/> từ chối (MACRO_BUDGET_EXCEEDED) thay vì che giấu.
    /// </summary>
    public static List<PlannedDay> Plan(ICollection<MacroProgramDayTarget> rows, MacroProgramWeek week)
    {
        int Kcal(MacroProgramDayTarget d) => d.IsFastingDay ? 0 : d.CaloriesTarget ?? week.CaloriesTarget;
        PlannedDay Same(MacroProgramDayTarget d) => d.IsFastingDay
            ? new(d, 0, 0, 0, 0, false)
            : new(d, Kcal(d), d.ProteinTarget ?? week.ProteinTarget, d.CarbsTarget ?? week.CarbsTarget, d.FatTarget ?? week.FatTarget, false);

        var budget = week.CaloriesTarget * 7;
        var fixedRows = rows.Where(d => d.IsLocked || d.IsFastingDay).ToList();
        var free = rows.Except(fixedRows).ToList();
        if (free.Count == 0 || Math.Abs(rows.Sum(Kcal) - budget) <= RoundingTolerance)
            return rows.Select(Same).ToList();

        var freeBudget = Math.Max(0, budget - fixedRows.Sum(Kcal));
        var freeSum = free.Sum(Kcal);
        var scaled = free.ToDictionary(d => d, d => freeSum > 0 ? (int)Math.Round(Kcal(d) * (decimal)freeBudget / freeSum, MidpointRounding.ToEven) : freeBudget / free.Count);
        var diff = freeBudget - scaled.Values.Sum();
        if (diff != 0) scaled[free.OrderByDescending(d => scaled[d]).First()] += diff;
        return rows.Select(d => scaled.TryGetValue(d, out var kcal) ? Scale(d, kcal, week) : Same(d)).ToList();
    }

    private static PlannedDay Scale(MacroProgramDayTarget d, int kcal, MacroProgramWeek week)
    {
        var oldKcal = d.CaloriesTarget ?? week.CaloriesTarget;
        var p = d.ProteinTarget ?? week.ProteinTarget;
        var c = d.CarbsTarget ?? week.CarbsTarget;
        var oldRemaining = oldKcal - 4m * p;
        var carbShare = oldRemaining > 0 ? Math.Clamp(4m * c / oldRemaining, 0m, 1m) : 0.5m;

        var remaining = kcal - 4m * p;   // có thể âm: KHÔNG kẹp về 0 và KHÔNG hạ protein để che
        return new(d, kcal, Math.Round(p, 1), Math.Round(carbShare * remaining / 4m, 1), Math.Round((1m - carbShare) * remaining / 9m, 1), true);
    }
}
