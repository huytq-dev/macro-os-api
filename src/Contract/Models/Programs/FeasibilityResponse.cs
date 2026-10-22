namespace Contract;

/// <summary>
/// Kết quả kiểm khả thi của target/program (MỘT dạng cho Preview, Create, UpdateGoal, EditGoal, SetManual và Check-in). Preview và check-in preview trả đúng object này trong response 200
/// (không ném lỗi) để FE cảnh báo trước khi tạo; Create/UpdateGoal/EditGoal/SetManual/Accept ném HTTP 400 với object này trong <c>data</c> và KHÔNG ghi gì.
/// BE không bao giờ tự đổi shift/protein/fat/sàn: chỉ trả đề xuất để user xác nhận. FE không tự tính shift.
/// </summary>
/// <param name="IsFeasible">true = mọi ngày hợp lệ; các trường còn lại rỗng/null.</param>
/// <param name="ReasonCode">Mã chính (phần tử đầu của <paramref name="ReasonCodes"/>); null khi khả thi.</param>
/// <param name="ReasonCodes">INVALID_CALORIE_TARGET (kcal ≤ 0 hoặc dưới sàn) | MACRO_BUDGET_EXCEEDED (4P+4C+9F vượt kcal quá dung sai, hoặc macro âm) | CALORIE_SHIFT_INFEASIBLE (shift tạo ngày không hợp lệ) | INFEASIBLE_PROGRAM (không có cấu hình hợp lệ nào, kể cả chia đều).</param>
/// <param name="AffectedDays">Các thứ có vi phạm (rỗng nếu vi phạm ở target tuần).</param>
/// <param name="CurrentShiftPercent">% shift đang cấu hình (null nếu không shift).</param>
/// <param name="SuggestedShiftPercent">% shift LỚN NHẤT còn hợp lệ với đúng các ngày đã chọn (tính bằng kiểm lại, bước 0.01, không dưới mức tối thiểu cho phép); null nếu không có hoặc không áp dụng.</param>
/// <param name="SuggestedAction">REDUCE_SHIFT | USE_EVEN_DISTRIBUTION | ADJUST_GOAL_RATE | UNLOCK_DAYS | null.</param>
/// <param name="Actions">Các cách chỉnh user có thể chọn: ADJUST_GOAL_RATE | ADJUST_CALORIES | ADJUST_MACRO_PREFERENCES | REDUCE_SHIFT | USE_EVEN_DISTRIBUTION | UNLOCK_DAYS.</param>
/// <param name="CurrentDailyTargets">7 ngày với cấu hình hiện tại (kể cả ngày không hợp lệ, carbs có thể âm) để so sánh "trước".</param>
/// <param name="ProposedDailyTargets">7 ngày nếu áp dụng <paramref name="SuggestedAction"/> (đã kiểm hợp lệ); rỗng nếu không có đề xuất. Chỉ áp dụng khi user xác nhận.</param>
/// <param name="Warnings">Cảnh báo KHÔNG chặn (luôn là mảng, rỗng nếu không có). Hiện chỉ MACRO_KCAL_MISMATCH cho target user tự nhập (Manual/Collaborative); IsFeasible vẫn true và Save không bị chặn, BE không đổi giá trị của user. Target do thuật toán/chia lại tạo ra thì KHÔNG cảnh báo mà bị từ chối (Violations).</param>
/// <param name="Message">Câu tóm tắt hành động được cho user (tiếng Anh, theo convention validation).</param>
public sealed record FeasibilityResponse(
    bool IsFeasible,
    string? ReasonCode,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<FeasibilityViolationModel> Violations,
    IReadOnlyList<string> AffectedDays,
    decimal? CurrentShiftPercent,
    decimal? SuggestedShiftPercent,
    string? SuggestedAction,
    IReadOnlyList<string> Actions,
    IReadOnlyList<ProgramDayModel> CurrentDailyTargets,
    IReadOnlyList<ProgramDayModel> ProposedDailyTargets,
    string? Message,
    IReadOnlyList<FeasibilityWarningModel>? Warnings = null);
