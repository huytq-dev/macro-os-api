namespace Application;

public sealed record MacroTargets(int Calories, decimal ProteinG, decimal CarbsG, decimal FatG);

/// <summary>ALG-01: TDEE khởi điểm (Mifflin-St Jeor × activity) và chia macro theo diet style. Hàm thuần, không I/O.</summary>
public static class MacroTargetCalculator
{
    public static decimal MifflinBmr(Gender gender, int ageYears, decimal heightCm, decimal weightKg)
        => 10m * weightKg + 6.25m * heightCm - 5m * ageYears + (gender == Gender.Male ? 5m : -161m);

    public static decimal ActivityFactor(ActivityLevel activity) => activity switch
    {
        ActivityLevel.Sedentary        => 1.2m,
        ActivityLevel.LightlyActive    => 1.375m,
        ActivityLevel.ModeratelyActive => 1.55m,
        ActivityLevel.VeryActive       => 1.725m,
        _                              => 1.9m,
    };

    public static int InitialTdee(Gender gender, int ageYears, decimal heightCm, decimal weightKg, ActivityLevel activity)
        => (int)Math.Round(MifflinBmr(gender, ageYears, heightCm, weightKg) * ActivityFactor(activity));

    public static decimal ProteinGPerKg(ProteinLevel level, TrainingType training)
    {
        var baseValue = level switch
        {
            ProteinLevel.Low       => 1.6m,
            ProteinLevel.Moderate  => 1.8m,
            ProteinLevel.High      => 2.0m,
            _                      => 2.4m,
        };
        // Có tập tạ thì cộng thêm 0.2 g/kg
        return training is TrainingType.Lifting or TrainingType.Both ? baseValue + 0.2m : baseValue;
    }

    /// <summary>kcal/ngày từ TDEE và rate mục tiêu (% cân nặng/tuần, dương = giảm).</summary>
    public static int DailyCalories(int tdee, decimal ratePercentPerWeek, decimal weightKg, int kcalPerKg, int? floorKcal)
    {
        var rateKg = ratePercentPerWeek / 100m * weightKg;
        var kcal = (int)Math.Round(tdee - rateKg * kcalPerKg / 7m);
        return floorKcal is { } floor ? Math.Max(kcal, floor) : kcal;
    }

    public static MacroTargets SplitMacros(int calories, decimal weightKg, decimal proteinGPerKg, decimal? fixedProteinG, DietStyle style)
    {
        var protein = fixedProteinG ?? Math.Round(proteinGPerKg * weightKg, 1);
        var fatMin = Math.Max(0.5m * weightKg, calories * 0.20m / 9m);

        decimal fat, carbs;
        switch (style)
        {
            case DietStyle.LowFat:
                fat = fatMin;
                carbs = Remaining(calories, protein, fat);
                break;
            case DietStyle.LowCarb:
                carbs = Math.Min(calories * 0.30m / 4m, 200m);
                fat = Math.Max((calories - 4m * protein - 4m * carbs) / 9m, fatMin);
                break;
            case DietStyle.Keto:
                carbs = 30m;
                fat = Math.Max((calories - 4m * protein - 4m * carbs) / 9m, fatMin);
                break;
            default: // Balanced
                fat = Math.Max(calories * 0.30m / 9m, fatMin);
                carbs = Remaining(calories, protein, fat);
                break;
        }

        return new MacroTargets(calories, protein, Math.Round(Math.Max(carbs, 0), 1), Math.Round(fat, 1));
    }

    private static decimal Remaining(int calories, decimal protein, decimal fat)
        => (calories - 4m * protein - 9m * fat) / 4m;

    public static int AgeYears(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        return dateOfBirth > today.AddYears(-age) ? age - 1 : age;
    }
}
