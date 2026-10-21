namespace Contract;

/// <param name="HoldingReason">Lý do Holding của điểm mới nhất; null nếu không Holding.</param>
public sealed record ExpenditureResponse(
    ExpenditurePointModel? Current,
    string? HoldingReason,
    DateOnly? ExpenditureStartDate,
    IReadOnlyList<ExpenditurePointModel> Points
);
