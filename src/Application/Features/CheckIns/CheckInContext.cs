namespace Application;

/// <param name="HasProposal">false khi expenditure đang Calibrating/Holding: preview không có đề xuất, Accept giữ nguyên target tuần trước.</param>
public sealed record CheckInContext(
    MacroProgram Program,
    MacroProgramWeek PrevWeek,
    ExpenditureSnapshot Tdee,
    decimal TrendNow,
    CheckInProposal Proposal,
    bool HasProposal,
    DateOnly? LastCheckIn,
    DateOnly PeriodStart,
    IReadOnlyList<CheckInDayModel> Days,
    CheckInPreviewResponse Preview
);
