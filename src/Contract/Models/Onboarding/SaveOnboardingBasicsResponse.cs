namespace Contract;

/// <summary>
/// Kết quả lưu Basics. Onboarding KHÔNG BAO GIỜ ghi đè lần cân hôm nay đã có: nếu user nhập cân nặng KHÁC lần cân đã lưu (so theo số hiển thị làm tròn 1 chữ số thập phân trong đơn vị của user, kg hoặc lb) thì giữ nguyên số đã lưu và báo qua <paramref name="WeightKept"/>.
/// </summary>
/// <param name="WeightKept">true CHỈ KHI request có cân nặng, hôm nay (ngày local) đã có lần cân và số đó khác số vừa nhập (theo quy tắc trên) ⇒ FE hiện thông báo ngắn "cân nặng đã lưu được giữ nguyên". Trùng số, chưa có lần cân (vừa tạo từ số user nhập) hoặc request không có cân nặng ⇒ false.</param>
/// <param name="StoredWeightKg">Cân nặng đã lưu của hôm nay (kg, chính xác) khi request có cân nặng; null khi request không có cân nặng.</param>
/// <param name="StoredWeightDisplay">Cùng số đó theo đơn vị user, làm tròn 1 chữ số thập phân (số FE nên hiện trong thông báo); null khi <paramref name="StoredWeightKg"/> null.</param>
/// <param name="DisplayUnit">kg | lb (UnitPreference của user sau lần lưu này).</param>
public sealed record SaveOnboardingBasicsResponse(bool WeightKept, decimal? StoredWeightKg, decimal? StoredWeightDisplay, string DisplayUnit);
