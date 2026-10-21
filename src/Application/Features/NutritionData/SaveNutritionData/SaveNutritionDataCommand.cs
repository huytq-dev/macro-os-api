namespace Application;

/// <summary>
/// Lưu MỘT ngày từ sheet Nutrition Data Manager (Date trong body hoặc route; không ở tương lai theo múi giờ user):
/// • Ngày có món log từ Food Log (Source = App, có entry không phải Quick Add): KHÔNG ghi đè/xoá món — chỉ đổi được <c>IsIncomplete</c>; gửi Calories/macros hoặc IsFasting = true → 400.
/// • <c>IsFasting</c> = true (ngày trống / nhập tay): Calories/macros phải trống hoặc 0; entry nhập tay của ngày bị xoá mềm, ngày tính là đã log với 0 kcal (không phá chuỗi).
/// • Có <c>Calories</c> (không fasting; ngày trống / nhập tay): tạo/cập nhật đúng MỘT entry nhập tay (Quick Add; macros thiếu = 0) thay các entry nhập tay cũ — tổng ngày = số nhập.
/// • Không có Calories và không fasting: chỉ đổi cờ <c>IsIncomplete</c> của ngày đã có dữ liệu (không đụng entry); ngày chưa có gì thì 400; ngày đang fasting mà bỏ fasting thì thành ngày chưa log.
/// <c>IsIncomplete</c> = true: expenditure và trung bình check-in bỏ qua ngày này. Trả lại ngày sau khi lưu; expenditure/aggregate dựng lại nền như POST entries.
/// </summary>
public sealed record SaveNutritionDataCommand(
    DateOnly? Date,
    decimal? Calories,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG,
    bool IsFasting = false,
    bool IsIncomplete = false
) : IRequest<NutritionDataDayModel>;
