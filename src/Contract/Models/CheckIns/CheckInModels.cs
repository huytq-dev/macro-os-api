namespace Contract;

/// <param name="RateActualKgPerWeek">Δtrend 7 ngày (dương = giảm, cùng chiều rate mục tiêu).</param>
public sealed record CheckInWeekSummary(
    decimal AvgCaloriesIn,
    int DaysLogged,
    int DaysMarkedIncomplete,
    decimal? TrendStartKg,
    decimal? TrendNowKg,
    decimal? RateActualKgPerWeek,
    decimal RateTargetKgPerWeek,
    decimal TdeeKcal,
    string ExpenditureStatus
);

public sealed record CheckInProposalModel(
    int PreviousCalories,
    int NewCalories,
    int Delta,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    bool Clamped,
    bool FloorApplied,
    string Reason,
    int? WeeklyBudgetKcal
);

/// <summary>Số liệu cho template lý do ở FE (i18n). New* = Old* khi không có đề xuất.</summary>
public sealed record CheckInReasonParams(
    decimal AvgKcalIn,
    int DaysLogged,
    int DaysMarkedIncomplete,
    decimal? WeightChangeKg,
    int OldKcal,
    int NewKcal,
    decimal OldProteinG,
    decimal OldCarbsG,
    decimal OldFatG,
    decimal NewProteinG,
    decimal NewCarbsG,
    decimal NewFatG
);

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

/// <param name="Key">intro | partial-logging | program-update; thứ tự mảng = thứ tự hiển thị.</param>
/// <param name="State">required | skippable | hidden.</param>
public sealed record CheckInModuleModel(string Key, string State);

/// <param name="Kcal">null = chưa log ngày đó.</param>
/// <param name="TargetKcal">Target kcal hiệu lực của ngày đó (DayTarget hoặc target tuần); null nếu chưa có target.</param>
/// <param name="SuspectedIncomplete">Đã log nhưng 0 &lt; kcal &lt; tỉ lệ × TDEE của ngày đó.</param>
/// <param name="MarkedIncomplete">User đã đánh dấu; expenditure bỏ qua ngày này.</param>
public sealed record CheckInDayModel(DateOnly Date, decimal? Kcal, int? TargetKcal, bool SuspectedIncomplete, bool MarkedIncomplete);

/// <param name="Days">7 ngày gần nhất, cũ → mới, kết thúc hôm nay.</param>
public sealed record CheckInSessionResponse(
    DateOnly NextCheckInDate,
    bool IsDue,
    bool AlreadyCheckedInToday,
    IReadOnlyList<CheckInModuleModel> Modules,
    IReadOnlyList<CheckInDayModel> Days,
    CheckInPreviewResponse Preview
);

public sealed record CheckInHistoryModel(
    Guid Id,
    DateOnly CheckInDate,
    decimal TdeeEstimate,
    decimal? TrendWeightKg,
    decimal? WeightChangeRateKgPerWeek,
    int PreviousCaloriesTarget,
    int NewCaloriesTarget,
    int CaloriesDelta,
    bool Accepted,
    int? WeeklyBudgetKcal
);
