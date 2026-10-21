namespace Application;

/// <summary>Một ngày trong editor theo ngày (Collaborative) hoặc bộ target nhập tay (Manual).</summary>
public sealed record DayTargetInput(DayOfWeek DayOfWeek, int Calories, decimal ProteinG, decimal CarbsG, decimal FatG, bool IsLocked = false, bool IsFastingDay = false);
