namespace Application;

/// <param name="KcalMax">Trần kcal/ngày ở editor ("set between 0 and N kcal").</param>
/// <param name="ProteinPerKgMax">Trần protein g/kg cân nặng.</param>
public sealed record ProgramLimits(int KcalMax, decimal ProteinPerKgMax);
