namespace Application;

/// <summary>
/// Lưu MỘT ngày từ sheet Nutrition Data Manager / Edit Day (Date trong body hoặc route; không ở tương lai theo múi giờ user). Manual Nutrition (S29) là dữ liệu CỦA NGÀY, lưu ở 4 cột riêng của FoodLogs,
/// KHÔNG phải món ăn: không tạo/sửa/xoá FoodLogEntry, ghi chú, nước. Override thắng Food Log (không cộng); null = chưa nhập, 0 = nhập 0.
/// • Có ít nhất một trong Calories/ProteinG/FatG/CarbsG: đặt override của ngày bằng ĐÚNG các giá trị gửi lên (trường bỏ trống = null, không lấy từ Food Log, không thành 0; không tự tính calories từ macro).
///   Cho phép calories-only, macro-only, 0. Cho phép trên ngày đã có món. Gửi đúng số đang lưu ⇒ không ghi gì.
/// • Không có trường manual nào: chỉ đổi cờ (IsFasting/IsIncomplete). Gửi tất cả trống mà không đổi gì ⇒ 400 (NOTHING_TO_SAVE, hoặc USE_REMOVE_OVERRIDE nếu ngày đang có override): KHÔNG xoá override, KHÔNG báo thành công.
/// • IsFasting/IsIncomplete: null = giữ nguyên (trước đây thiếu = false làm mất cờ). Nhịn ăn + manual dương (calories hay macro) ⇒ 400 FASTING_WITH_MANUAL_NUTRITION; bật nhịn ăn trên ngày đã có entry ⇒ 409.
/// IsIncomplete = true: expenditure bỏ qua ngày này (chỉ đặt được trên ngày đã có dữ liệu). Ngày thành trống thật thì cờ chưa-đủ tự bỏ. Trả lại ngày sau khi lưu.
/// </summary>
public sealed record SaveNutritionDataCommand(
    DateOnly? Date,
    decimal? Calories,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG,
    bool? IsFasting = null,
    bool? IsIncomplete = null
) : IRequest<NutritionDataDayModel>;
