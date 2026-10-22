namespace Contract;

/// <param name="Proposal">null khi expenditure đang Calibrating/Holding: Accept lúc đó giữ nguyên target tuần trước.</param>
/// <param name="Changed">Có đề xuất và kcal/macro mới khác tuần trước.</param>
/// <param name="ReasonCodes">CALIBRATING | HOLDING | INCOMPLETE_DAYS | FASTER_THAN_TARGET | SLOWER_THAN_TARGET | ON_TARGET (theo HƯỚNG của goal: Loss giảm nhiều hơn = FASTER; Gain/MuscleGain tăng nhiều hơn = FASTER; Maintain dùng LOSING_WEIGHT | GAINING_WEIGHT | ON_TARGET) | CLAMPED | FLOOR_APPLIED | INFEASIBLE_PROPOSAL (xem Feasibility).</param>
/// <param name="Feasibility">Kết quả kiểm khả thi của đề xuất (kcal &gt; 0, macro, shift, 7 ngày). IsFeasible = false: Accept bị từ chối 400 (không ghi gì, target đã chấp nhận được giữ); user chọn đề xuất trong Feasibility (shift nhỏ hơn / chia đều) qua cập nhật chương trình rồi xem lại.</param>
public sealed record CheckInPreviewResponse(
    DateOnly NextCheckInDate,
    bool IsDue,
    bool AlreadyCheckedInToday,
    CheckInWeekSummary Week,
    CheckInProposalModel? Proposal,
    bool Changed,
    IReadOnlyList<string> ReasonCodes,
    CheckInReasonParams ReasonParams,
    FeasibilityResponse? Feasibility = null
);
