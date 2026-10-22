namespace Domain;

// Cài đặt expenditure THEO USER, độc lập MacroProgram (1 dòng/user, tạo lười). Giá trị NULL = chưa cấu hình ⇒ hành vi cũ (lấy từ program mới nhất / công thức), tạo dòng KHÔNG được ghi đè gì.
// Static/Dynamic nằm ở ExpenditureModePeriod (nguồn duy nhất); TDEE học được nằm ở ExpenditureSnapshot.
public class UserExpenditureSettings : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public InitialEstimateMode? InitialEstimateMode { get; set; }   // null = chưa cấu hình (dùng giá trị từ program như trước)
    public int? ManualInitialTdeeKcal { get; set; }                 // chỉ khi InitialEstimateMode = Manual
    public BmrEquationPreference BmrEquation { get; set; } = BmrEquationPreference.MacroFactor;
    public BiologicalSex? BmrSex { get; set; }                      // user tự xác nhận, chỉ cho Equation 1 khi cần; không map từ Gender, không mặc định
    public DateOnly? DefaultCalculationStartDate { get; set; }      // ngày local expenditure tracking THỰC SỰ khởi tạo lần đầu; ghi một lần, độc lập program/check-in/mục tiêu, không bao giờ đổi; null = chưa có bằng chứng đáng tin (legacy), KHÔNG bịa
    public DateOnly? CalculationStartDate { get; set; }           // null = dùng ExpenditureStartDate của program mới nhất
    public int CalculationRevision { get; set; }                    // tăng khi đổi Start Date; gắn vào snapshot dựng lại
    public byte[] RowVersion { get; set; } = null!;

    public User User { get; set; } = null!;
}
