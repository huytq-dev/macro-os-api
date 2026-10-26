namespace Contract;

/// <param name="HoldingReason">Lý do chưa Updating của điểm mới nhất (Holding hoặc Calibrating, gồm "cần ≥ 2 lần cân trong cửa sổ tính"); null nếu đang Updating.</param>
/// <param name="FluxModel">Phiên bản chính sách Flux của các dải trong Current/Points (<c>MACROOS_FLUX_V1</c>); null khi không điểm nào có dải (mặc định LUÔN null: Algorithm:FluxRangeEnabled = false). Dải là MacroOS approximation, MF không công bố công thức flux.</param>
public sealed record ExpenditureResponse(
    ExpenditurePointModel? Current,
    string? HoldingReason,
    DateOnly? ExpenditureStartDate,
    IReadOnlyList<ExpenditurePointModel> Points,
    string? FluxModel = null
);
