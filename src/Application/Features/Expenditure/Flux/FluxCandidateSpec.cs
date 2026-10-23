namespace Application;

/// <summary>
/// Một quỹ đạo ứng viên của Flux: CÙNG cấu trúc với bộ ước lượng production (ước lượng cân bằng năng lượng trong cửa sổ <see cref="WindowDays"/> ngày, rồi làm mượt EMA hệ số <see cref="Beta"/>),
/// chỉ khác độ nhạy. Production = (β 0.1, W 21) (<c>ExpenditureCalculator.ComputeRaw/Smooth</c>, <c>ExpenditureRebuilder.cs:121,132</c>). Ứng viên KHÔNG đổi trend cân nặng.
/// </summary>
public sealed record FluxCandidateSpec(string Name, decimal Beta, int WindowDays);
