namespace Contract;

/// <param name="Days">7 ngày gần nhất, cũ → mới, kết thúc hôm nay.</param>
public sealed record CheckInSessionResponse(
    DateOnly NextCheckInDate,
    bool IsDue,
    bool AlreadyCheckedInToday,
    IReadOnlyList<CheckInModuleModel> Modules,
    IReadOnlyList<CheckInDayModel> Days,
    CheckInPreviewResponse Preview
);
