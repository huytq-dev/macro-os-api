namespace Application;

/// <summary>Quy đổi đáp án wizard sang ActivityLevel/TrainingType và ước tính TDEE khởi điểm. Hàm thuần, không I/O.</summary>
public static class OnboardingCalculator
{
    /// <summary>Điểm = mức vận động ngày (0–2) + nhóm số buổi tập (0–3); 0 → Sedentary … 5 → ExtraActive.</summary>
    public static ActivityLevel ActivityFrom(DailyActivityBand daily, ExerciseSessionsBand sessions)
        => ((int)daily + (int)sessions) switch
        {
            0 => ActivityLevel.Sedentary,
            1 or 2 => ActivityLevel.LightlyActive,
            3 => ActivityLevel.ModeratelyActive,
            4 => ActivityLevel.VeryActive,
            _ => ActivityLevel.ExtraActive,
        };

    public static TrainingType TrainingFrom(ExperienceLevel? lifting, ExperienceLevel? cardio)
    {
        var lifts = lifting is > ExperienceLevel.None;
        var runs = cardio is > ExperienceLevel.None;
        return (lifts, runs) switch
        {
            (true, true) => TrainingType.Both,
            (true, false) => TrainingType.Lifting,
            (false, true) => TrainingType.Cardio,
            _ => TrainingType.None,
        };
    }

    /// <summary>Giữa khoảng của mỗi mức mỡ; mức cuối (50%+) lấy 52.</summary>
    public static decimal BodyFatPercent(BodyFatBand band) => band switch
    {
        BodyFatBand.Bf10To13 => 11.5m,
        BodyFatBand.Bf14To17 => 15.5m,
        BodyFatBand.Bf18To23 => 20.5m,
        BodyFatBand.Bf24To28 => 26m,
        BodyFatBand.Bf29To33 => 31m,
        BodyFatBand.Bf34To37 => 35.5m,
        BodyFatBand.Bf38To42 => 40m,
        BodyFatBand.Bf43To49 => 46m,
        _ => 52m,
    };

    /// <summary>
    /// Có tỉ lệ mỡ: Katch-McArdle (370 + 21.6 × lean mass); không có: Mifflin-St Jeor. Sau đó × hệ số hoạt động.
    /// Sai số điển hình 100–200 kcal; thuật toán expenditure sẽ tự hiệu chỉnh theo dữ liệu thực.
    /// </summary>
    public static TdeeEstimate EstimateTdee(UserProfile profile, decimal weightKg, DateOnly today)
    {
        var age = MacroTargetCalculator.AgeYears(profile.DateOfBirth!.Value, today);
        decimal bmr;
        string method;
        if (profile.BodyFat is { } band)
        {
            var leanMass = weightKg * (1m - BodyFatPercent(band) / 100m);
            bmr = 370m + 21.6m * leanMass;
            method = "KatchMcArdle";
        }
        else
        {
            bmr = MacroTargetCalculator.MifflinBmr(profile.Gender, age, profile.HeightCm!.Value, weightKg);
            method = "MifflinStJeor";
        }

        var tdee = (int)Math.Round(bmr * MacroTargetCalculator.ActivityFactor(profile.ActivityLevel));
        return new TdeeEstimate(tdee, (int)Math.Round(bmr), method);
    }
}
