namespace Application;

/// <summary>
/// CHÍNH SÁCH MacroOS (không phải của MacroFactor) chọn phương trình từ dữ liệu có thật; các công thức bên trong là bản đã xác minh (<see cref="BmrEquations"/>):
/// Cunningham khi user chọn và tính được FFM; Equation 3 khi FFM hợp lệ VÀ user xác nhận ≥ 7 giờ/tuần tập nặng (không suy từ số buổi tập hay Athletic Pursuits);
/// Equation 2 khi có FM + FFM hợp lệ và biết tuổi; Equation 1 khi biết cân, cao, tuổi, giới (nam/nữ); còn lại None.
/// Hàm thuần, chưa nối vào luồng nào.
/// </summary>
public static class BmrEquationSelector
{
    public const decimal AthleteHoursPerWeek = 7m;

    public static BmrSelection Select(
        decimal weightKg, decimal? heightCm, int? ageYears, Gender? gender,
        decimal? bodyFatPercent, decimal? intenseExerciseHoursPerWeek, bool preferCunningham, BiologicalSex? bmrSex = null)
    {
        if (weightKg <= 0) return new(BmrEquationKind.None, null, "Cần cân nặng hợp lệ.");

        var hasComposition = bodyFatPercent is > 0 and < 100;
        decimal ffm = 0, fm = 0;
        if (hasComposition)
        {
            fm = weightKg * bodyFatPercent!.Value / 100m;
            ffm = weightKg - fm;
        }

        if (preferCunningham)
        {
            if (!hasComposition) return new(BmrEquationKind.None, null, "Cunningham cần tỉ lệ mỡ để tính FFM.");
            return new(BmrEquationKind.Cunningham, BmrEquations.Cunningham(ffm), null);
        }

        if (hasComposition && intenseExerciseHoursPerWeek is { } hours && hours >= AthleteHoursPerWeek)
            return new(BmrEquationKind.Athlete, BmrEquations.Athlete(ffm), null);

        if (hasComposition && ageYears is { } age)
            return new(BmrEquationKind.BodyComposition, BmrEquations.BodyComposition(ffm, fm, age), null);

        // Số hạng giới của Eq1: tham số sinh học user xác nhận (BmrSex) ưu tiên; không có thì dùng Gender Nam/Nữ; "Other"/chưa có thì KHÔNG đoán, không lấy trung bình hai hệ số.
        BiologicalSex? sex = bmrSex ?? gender switch { Gender.Male => BiologicalSex.Male, Gender.Female => BiologicalSex.Female, _ => null };
        if (heightCm is > 0 && ageYears is { } a && sex is { } s)
            return new(BmrEquationKind.General, BmrEquations.General(weightKg, heightCm.Value, a, s == BiologicalSex.Female), null);

        return new(BmrEquationKind.None, null, "Thiếu dữ liệu: Equation 1 cần chiều cao, tuổi và tham số sinh học nam/nữ (user tự xác nhận khi giới là Other); Equation 2 cần tỉ lệ mỡ + tuổi. Hãy bổ sung hoặc nhập Manual Initial Estimate.");
    }
}
