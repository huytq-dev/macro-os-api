namespace Contract;

/// <param name="Proposal">null khi expenditure đang Calibrating/Holding: Accept lúc đó giữ nguyên target tuần trước.</param>
/// <param name="Changed">Có đề xuất và kcal/macro mới khác tuần trước.</param>
/// <param name="ReasonCodes">CALIBRATING | HOLDING | INCOMPLETE_DAYS | FASTER_THAN_TARGET | SLOWER_THAN_TARGET | ON_TARGET | CLAMPED | FLOOR_APPLIED.</param>
public sealed record CheckInPreviewResponse(
    DateOnly NextCheckInDate,
    bool IsDue,
    bool AlreadyCheckedInToday,
    CheckInWeekSummary Week,
    CheckInProposalModel? Proposal,
    bool Changed,
    IReadOnlyList<string> ReasonCodes,
    CheckInReasonParams ReasonParams
);
