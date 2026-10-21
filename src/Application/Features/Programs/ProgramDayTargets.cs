namespace Application;

/// <summary>Chia lại ngân sách tuần cho 7 dòng MacroProgramDayTarget khi target tuần đổi (check-in, đổi goal, nhập TDEE). Hàm thuần.</summary>
public static class ProgramDayTargets
{
    // Sai lệch ≤ ngưỡng này giữa tổng 7 ngày và 7 × target tuần chỉ là sai số làm tròn của trung bình, không chia lại
    private const int RoundingTolerance = 3;

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
    public static void Rebalance(MacroProgram program, MacroProgramWeek week)
    {
        var rows = program.DayTargets.ToList();
        if (rows.Count == 0) return;

        int Kcal(MacroProgramDayTarget d) => d.IsFastingDay ? 0 : d.CaloriesTarget ?? week.CaloriesTarget;
        var budget = week.CaloriesTarget * 7;
        var fixedRows = rows.Where(d => d.IsLocked || d.IsFastingDay).ToList();
        var free = rows.Except(fixedRows).ToList();

        if (free.Count > 0 && Math.Abs(rows.Sum(Kcal) - budget) > RoundingTolerance)
        {
            var freeBudget = Math.Max(0, budget - fixedRows.Sum(Kcal));
            var freeSum = free.Sum(Kcal);
            var scaled = free.ToDictionary(d => d, d => freeSum > 0 ? (int)Math.Round(Kcal(d) * (decimal)freeBudget / freeSum, MidpointRounding.ToEven) : freeBudget / free.Count);
            var diff = freeBudget - scaled.Values.Sum();
            if (diff != 0) scaled[free.OrderByDescending(d => scaled[d]).First()] += diff;
            foreach (var d in free) Apply(d, scaled[d], week);
        }

        var avg = Average(rows, week);
        week.CaloriesTarget = avg.Calories;
        week.ProteinTarget = avg.ProteinG;
        week.CarbsTarget = avg.CarbsG;
        week.FatTarget = avg.FatG;
    }

    private static void Apply(MacroProgramDayTarget d, int kcal, MacroProgramWeek week)
    {
        var oldKcal = d.CaloriesTarget ?? week.CaloriesTarget;
        var p = d.ProteinTarget ?? week.ProteinTarget;
        var c = d.CarbsTarget ?? week.CarbsTarget;
        var oldRemaining = oldKcal - 4m * p;
        var carbShare = oldRemaining > 0 ? Math.Clamp(4m * c / oldRemaining, 0m, 1m) : 0.5m;

        var protein = Math.Min(p, kcal / 4m);
        var remaining = Math.Max(0m, kcal - 4m * protein);
        d.CaloriesTarget = kcal;
        d.ProteinTarget = Math.Round(protein, 1);
        d.CarbsTarget = Math.Round(carbShare * remaining / 4m, 1);
        d.FatTarget = Math.Round((1m - carbShare) * remaining / 9m, 1);
    }
}
