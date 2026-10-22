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

    /// <summary>Cunningham (khớp MacroFactor): BMR = 500 + 22 × lean body mass (kg).</summary>
    public static decimal CunninghamBmr(decimal leanMassKg) => 500m + 22m * leanMassKg;

    /// <summary>
    /// Hồ sơ có tỉ lệ mỡ (BodyFat của wizard, lấy giữa khoảng; KHÔNG dùng % mỡ đo hằng ngày): Cunningham từ lean mass = cân × (1 − %mỡ);
    /// không có: Mifflin-St Jeor (dự phòng của MacroOS, chưa kiểm chứng với MacroFactor). Sau đó × hệ số hoạt động.
    /// Chỉ là ước tính khởi điểm: không bao giờ ghi đè TDEE đã học hay TDEE user tự nhập (caller ưu tiên chúng); expenditure tự hiệu chỉnh theo dữ liệu thực.
    /// </summary>
    public static TdeeEstimate EstimateTdee(UserProfile profile, decimal weightKg, DateOnly today, BiologicalSex? bmrSex = null)
        => TryEstimateTdee(profile, weightKg, today, bmrSex, out var missing)
           ?? throw new BadRequestException(ExceptionMessages.InitialEstimateNeedsAnswers(string.Join(", ", missing)));

    /// <summary>
    /// Như <see cref="EstimateTdee"/> nhưng trả null + danh sách đầu vào thiếu thay vì ném lỗi (dùng khi TDEE đã học/nhập tay thắng công thức nên công thức không bắt buộc).
    /// Tính qua <see cref="InitialExpenditureCalculator"/> (bộ tính dùng chung): hệ số hoạt động MF từ hai câu trả lời Activity + Exercise (KHÔNG dùng ActivityLevel mặc định), Gender Other không bao giờ bị coi là nữ.
    /// <paramref name="today"/> phải là ngày LOCAL của user (tính tuổi).
    /// </summary>
    public static TdeeEstimate? TryEstimateTdee(UserProfile profile, decimal weightKg, DateOnly today, BiologicalSex? bmrSex, out IReadOnlyList<string> missing)
    {
        var r = InitialExpenditureCalculator.Compute(new InitialExpenditureInput(
            weightKg, profile.DateOfBirth, profile.HeightCm, profile.Gender, bmrSex, profile.BodyFat, profile.DailyActivity, profile.ExerciseSessions, today));
        missing = r.Missing;
        return r.IsComputable ? new TdeeEstimate(r.TdeeKcal!.Value, r.BmrKcal!.Value, r.Method, r.BodyFatPercent) : null;
    }
}
