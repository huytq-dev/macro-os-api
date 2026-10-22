namespace Application;

/// <summary>
/// Cập nhật từng phần (trường nào gửi mới đổi; không có "xoá về null"). Chỉ ảnh hưởng ước tính ban đầu: không reset/đổi expenditure đã học.
/// <c>WeighedMoreThanBefore</c> (Prior Weight: Yes | No | NotSure) cũng lưu cân LÚC trả lời để không diễn giải lại đáp án theo cân mới.
/// <c>InitialEstimateMode = Manual</c> cần <c>ManualInitialTdeeKcal</c> (1000–6000); <c>Formula</c> xoá giá trị nhập tay. <c>AthleticPursuits</c> nhận đúng 4 đáp án MF và không tự bật Equation 3.
/// </summary>
public sealed record UpdateExpenditureSettingsCommand(
    InitialEstimateMode? InitialEstimateMode,
    int? ManualInitialTdeeKcal,
    BmrEquationPreference? BmrEquation,
    BiologicalSex? BmrSex,
    AthleticPursuit? AthleticPursuits,
    WeightHistoryAnswer? WeighedMoreThanBefore = null,
    decimal? PriorWeightThresholdShown = null,
    string? PriorWeightThresholdUnit = null
) : IRequest<ExpenditureSettingsResponse>;
