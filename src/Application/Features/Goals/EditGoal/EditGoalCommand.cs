namespace Application;

/// <summary>
/// Áp dụng Edit Goal trong MỘT lần lưu: Goal (đích, tốc độ) + MacroProgram (rate) + target tuần hiện tại (Coached/Collaborative tính lại từ hôm nay; Manual không tính).
/// Đổi hướng goal (Loss ⇄ Gain ⇄ Maintenance) → <c>RequiresNewProgram = true</c> và KHÔNG đổi gì: FE chuyển sang wizard New Program.
/// Thay cặp UpdateGoalTarget + UpdateProgramGoal mà FE gọi rời (hai cái cũ vẫn giữ cho client cũ). Trả bản xem trước (changes = từ → tới) của lần sửa này.
/// </summary>
public sealed record EditGoalCommand(decimal? TargetWeightKg, decimal? WeeklyRatePercent) : IRequest<GoalEditPreviewResponse>;
