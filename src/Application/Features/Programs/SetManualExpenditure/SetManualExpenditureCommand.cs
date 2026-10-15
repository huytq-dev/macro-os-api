namespace Application;

/// <summary>UC-B05: nhập tay TDEE; thuật toán học lại từ hôm nay (ExpenditureStartDate).</summary>
public sealed record SetManualExpenditureCommand(int TdeeKcal) : IRequest<Unit>;
