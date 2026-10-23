namespace Application;

/// <summary>
/// Cân nặng của Onboarding (hàm thuần): Onboarding KHÔNG BAO GIỜ ghi đè hay xoá ghi chú của lần cân hôm nay đã có (theo ngày LOCAL của user). Nguồn sự thật của cân nặng là WeightEntry đã lưu: TDEE khởi điểm, program,
/// Goal và expenditure đều đọc lần cân mới nhất đã lưu, nên khi hôm nay đã có lần cân thì mọi thứ dùng ĐÚNG số đó (cả câu trả lời Prior Weight), không dùng thêm số onboarding khác ⇒ không có hai giá trị mâu thuẫn.
/// </summary>
public static class OnboardingWeight
{
    /// <param name="requested">Cân nặng user nhập ở onboarding (null = không gửi).</param>
    /// <param name="storedToday">Cân nặng lần cân HÔM NAY (local) đã lưu, chưa xoá; null = chưa có.</param>
    /// <param name="latestStored">Cân nặng lần cân gần nhất đã lưu (mọi ngày); null = chưa có lần nào.</param>
    /// <returns>
    /// Write: chỉ tạo lần cân hôm nay khi user gửi cân nặng VÀ hôm nay chưa có lần cân (mới tạo, không bao giờ cập nhật dòng có sẵn).
    /// WeightAtAnswer: cân nặng dùng cho câu trả lời Prior Weight = lần cân hôm nay đã lưu nếu có, ngược lại số user gửi, ngược lại lần cân gần nhất.
    /// </returns>
    public static (bool Write, decimal? WeightAtAnswer) Decide(decimal? requested, decimal? storedToday, decimal? latestStored)
        => (requested is not null && storedToday is null, storedToday ?? requested ?? latestStored);

    /// <summary>Số hiển thị theo đơn vị user, làm tròn 1 chữ số thập phân (kg hoặc lb).</summary>
    public static decimal Display(decimal weightKg, UnitSystem unit)
        => Math.Round(unit == UnitSystem.Imperial ? weightKg * PriorWeightQuestion.PoundsPerKg : weightKg, 1, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Dung sai "cùng số": hai cân nặng coi là TRÙNG khi số hiển thị (1 chữ số thập phân, đơn vị user) bằng nhau, tức lệch dưới 0.1 kg/lb sau làm tròn; nhập lb rồi quy ra kg không sinh thông báo giả.
    /// Kept = user gửi cân nặng, hôm nay đã có lần cân và hai số KHÁC nhau ⇒ báo "cân nặng đã lưu được giữ nguyên".
    /// </summary>
    public static bool Kept(decimal? requested, decimal? storedToday, UnitSystem unit)
        => requested is { } r && storedToday is { } s && Display(r, unit) != Display(s, unit);
}
