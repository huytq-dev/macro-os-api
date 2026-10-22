namespace Application;

/// <param name="Kind">None = thiếu dữ liệu, KHÔNG bịa số; <paramref name="Reason"/> nói thiếu gì.</param>
public sealed record BmrSelection(BmrEquationKind Kind, decimal? Bmr, string? Reason);
