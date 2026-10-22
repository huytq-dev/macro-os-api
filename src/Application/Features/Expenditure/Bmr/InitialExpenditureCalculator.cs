namespace Application;

/// <summary>
/// MỘT bộ tính BMR → TDEE khởi điểm cho onboarding, Preview, Create và Expenditure Settings (hàm thuần, không I/O).
/// Hệ số hoạt động: bảng MF đã công bố (cơ sở 1.2/1.4/1.6 theo bước + 0/0.1/0.2/0.3 theo buổi tập), <see cref="ActivityMultipliers"/>.
/// Chọn phương trình BMR (HIỆN TẠI = cách chọn cũ của MacroOS, CHƯA đổi sang MF Eq1/Eq2/Eq3 vì chính sách chọn đang chờ dữ liệu xác thực): có body fat band ⇒ Cunningham (lean mass = cân × (1 − midpoint)),
/// không có ⇒ Mifflin-St Jeor (dự phòng của MacroOS, chưa kiểm chứng với MF; cần tuổi, chiều cao, tham số sinh học nam/nữ). Thiếu đầu vào ⇒ <see cref="InitialExpenditureResult.Missing"/>, không có mặc định.
/// Chỉ là ước tính khởi điểm: không bao giờ ghi đè TDEE đã học hay TDEE user nhập tay (caller ưu tiên chúng).
/// </summary>
public static class InitialExpenditureCalculator
{
    public static InitialExpenditureResult Compute(InitialExpenditureInput i)
    {
        var missing = new List<string>();
        if (i.WeightKg <= 0) missing.Add("WeightKg");
        if (i.DailyActivity is null) missing.Add("DailyActivity");
        if (i.Exercise is null) missing.Add("ExerciseSessions");

        decimal? bmr = null; var method = ""; decimal? bodyFat = null;
        if (i.WeightKg > 0)
        {
            if (i.BodyFat is { } band)
            {
                bodyFat = OnboardingCalculator.BodyFatPercent(band);
                var lean = i.WeightKg * (1m - bodyFat.Value / 100m);
                if (lean > 0m) { bmr = OnboardingCalculator.CunninghamBmr(lean); method = "Cunningham"; }
            }
            else
            {
                // Mifflin cần tuổi, chiều cao và tham số nam/nữ: BmrSex user xác nhận, nếu không có thì Gender Nam/Nữ; Other/chưa có ⇒ thiếu (không đoán, không lấy trung bình)
                BiologicalSex? sex = i.BmrSex ?? i.Gender switch { Domain.Gender.Male => BiologicalSex.Male, Domain.Gender.Female => BiologicalSex.Female, _ => null };
                if (i.DateOfBirth is null) missing.Add("DateOfBirth");
                if (i.HeightCm is null) missing.Add("HeightCm");
                if (sex is null) missing.Add("Sex");
                if (i.DateOfBirth is { } dob && i.HeightCm is { } height && sex is { } s)
                {
                    var age = MacroTargetCalculator.AgeYears(dob, i.LocalToday);
                    bmr = MacroTargetCalculator.MifflinBmr(s == BiologicalSex.Male ? Domain.Gender.Male : Domain.Gender.Female, age, height, i.WeightKg);
                    method = "MifflinStJeor";
                }
            }
        }

        if (missing.Count > 0 || bmr is null) return new(null, null, method, bodyFat, 0m, missing);
        var multiplier = ActivityMultipliers.Total(i.DailyActivity!.Value, i.Exercise!.Value);
        return new((int)Math.Round(bmr.Value * i.AdaptationFactor * multiplier), (int)Math.Round(bmr.Value), method, bodyFat, multiplier, missing);
    }
}
