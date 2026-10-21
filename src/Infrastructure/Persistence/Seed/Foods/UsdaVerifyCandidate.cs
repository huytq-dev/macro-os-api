namespace Infrastructure;

/// <param name="Mismatch">Lý do bộ chấm điểm loại ứng viên (thương hiệu, từ thừa…); null = không bị loại.</param>
public sealed record UsdaVerifyCandidate(int FdcId, string Description, string DataType, decimal Score, string? Mismatch, decimal? Kcal, decimal? Protein, decimal? Fat, decimal? Carbs);
