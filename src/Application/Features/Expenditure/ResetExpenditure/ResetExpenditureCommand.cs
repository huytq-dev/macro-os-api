namespace Application;

/// <summary>UC-F04 (BR-11): học lại từ hôm nay — ExpenditureStartDate = today, TDEE₀ = TDEE mới nhất, Calibrating.</summary>
public sealed record ResetExpenditureCommand : IRequest<Unit>;
