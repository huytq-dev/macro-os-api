namespace Application;

/// <summary>
/// Các phương trình BMR của MacroFactor, ĐÃ XÁC MINH theo macrofactor.com/macrofactors-bmr (không thêm hệ số nào), và Cunningham 1980: <c>500 + 22 × FFM</c>.
/// Hàm thuần, chưa nối vào luồng nào; việc chọn phương trình tự động là CHÍNH SÁCH của MacroOS (<see cref="BmrEquationSelector"/>), không phải của MacroFactor.
/// </summary>
public static class BmrEquations
{
    /// <summary>Mốc tuổi của số hạng tuổi: dưới/bằng mốc hệ số nhỏ, vượt mốc hệ số lớn hơn.</summary>
    public const int AgeBreakpoint = 60;

    /// <summary>Equation 1 (không cần thành phần cơ thể): <c>129.6·W^0.55 + 0.011·H² − ageTerm − 213.8·Sex</c> (nam 0, nữ 1), <c>ageTerm = 1.96·min(age,60) + 4.9·max(0, age−60)</c>.</summary>
    public static decimal General(decimal weightKg, decimal heightCm, int ageYears, bool female)
    {
        var ageTerm = 1.96 * Math.Min(ageYears, AgeBreakpoint) + 4.9 * Math.Max(0, ageYears - AgeBreakpoint);
        var h = (double)heightCm;
        return Round(129.6 * Math.Pow((double)weightKg, 0.55) + 0.011 * h * h - ageTerm - (female ? 213.8 : 0));
    }

    /// <summary>Equation 2 (thành phần cơ thể, không có số hạng giới tính): <c>50.2·FFM^0.7 + 40.5·(FFM^0.7·FM^0.066) − [1.1·min(age,60) + 2.75·max(0, age−60)]</c>.</summary>
    public static decimal BodyComposition(decimal ffmKg, decimal fmKg, int ageYears)
    {
        var ffm07 = Math.Pow((double)ffmKg, 0.7);
        var ageTerm = 1.1 * Math.Min(ageYears, AgeBreakpoint) + 2.75 * Math.Max(0, ageYears - AgeBreakpoint);
        return Round(50.2 * ffm07 + 40.5 * (ffm07 * Math.Pow((double)fmKg, 0.066)) - ageTerm);
    }

    /// <summary>Equation 3 (vận động viên, ≥ 7 giờ/tuần tập nặng; không có số hạng tuổi/FM): <c>40.4·FFM^0.932</c>.</summary>
    public static decimal Athlete(decimal ffmKg) => Round(40.4 * Math.Pow((double)ffmKg, 0.932));

    /// <summary>Cunningham 1980: <c>500 + 22·FFM</c>.</summary>
    public static decimal Cunningham(decimal ffmKg) => OnboardingCalculator.CunninghamBmr(ffmKg);

    private static decimal Round(double v) => Math.Round((decimal)v, 2, MidpointRounding.ToEven);
}
