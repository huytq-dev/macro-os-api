namespace Application;

/// <summary>
/// Dải Flux một ngày, kcal nguyên: <c>LowerKcal = ⌊min⌋</c>, <c>UpperKcal = ⌈max⌉</c> của mọi quỹ đạo ứng viên VÀ TDEE đã học, nên <c>Lower ≤ TDEE đã học (làm tròn) ≤ Upper</c> luôn đúng.
/// Null cả hai khi <see cref="State"/> là Calibrating / NoObservations. <see cref="CandidateKcal"/> (chưa làm tròn, theo thứ tự policy) chỉ để chẩn đoán/nghiên cứu.
/// </summary>
public sealed record FluxPoint(DateOnly Date, decimal? LowerKcal, decimal? UpperKcal, FluxBandState State, IReadOnlyList<decimal> CandidateKcal);
