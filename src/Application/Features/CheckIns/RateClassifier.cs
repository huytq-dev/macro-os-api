namespace Application;

/// <param name="State">ON_TARGET | FASTER_THAN_TARGET | SLOWER_THAN_TARGET | LOSING_WEIGHT | GAINING_WEIGHT (Maintenance) | UNCERTAIN | WAITING.</param>
/// <param name="Deviation">Độ lệch (kg/tuần) theo hướng goal: dương = nhanh hơn mục tiêu; Maintenance: tốc độ thực tế (dương = tăng cân).</param>
/// <param name="HalfWidth">k·λ·SE + sai lệch khởi động (nửa bề rộng khoảng bất định).</param>
/// <param name="Reason">Lý do UNCERTAIN/WAITING: INSUFFICIENT_WEIGHT_DATA | STALE_WEIGHT_DATA | NOISE_NOT_ESTIMABLE | WARMUP_BIAS; null khi đã phân loại.</param>
public sealed record RateClassification(string State, double? Deviation, double? HalfWidth, double? Lower, double? Upper, string? Reason);

/// <summary>
/// Phân loại tốc độ thay đổi cân so với mục tiêu có xét độ bất định (MACROOS POLICY, KHÔNG phải MacroFactor; INACTIVE, chưa có caller production). Hàm thuần, CHỈ hiển thị: không bao giờ đổi kcal, macro, expenditure, đề xuất check-in hay TDEE đã học.
/// Hai khái niệm tách biệt: δ = độ lệch chấp nhận được (chính sách), k·λ·SE = độ bất định đo lường.
/// Với <c>dev</c> = độ lệch theo hướng goal (Loss: mục tiêu − thực tế; Gain/MuscleGain: thực tế − mục tiêu; dương = nhanh hơn) và h = nửa bề rộng:
/// ON_TARGET chỉ khi TOÀN BỘ khoảng [dev − h, dev + h] nằm trong [−δ, +δ]; FASTER chỉ khi dev − h &gt; δ; SLOWER chỉ khi dev + h &lt; −δ; còn lại UNCERTAIN.
/// WAITING khi chưa qua cổng dữ liệu TRA (<see cref="WeightTrendSeries.RateGate"/>). Maintenance (mục tiêu 0): ON_TARGET / LOSING_WEIGHT / GAINING_WEIGHT / UNCERTAIN.
/// </summary>
public static class RateClassifier
{
    public const string OnTarget = "ON_TARGET", Faster = "FASTER_THAN_TARGET", Slower = "SLOWER_THAN_TARGET", Losing = "LOSING_WEIGHT", Gaining = "GAINING_WEIGHT", Uncertain = "UNCERTAIN", Waiting = "WAITING";
    public const string NoiseNotEstimable = "NOISE_NOT_ESTIMABLE", WarmupBias = "WARMUP_BIAS";

    /// <summary>Sai lệch tối đa có thể có do khởi động: |φ|·|r̂| / (1 − |φ|) (r̂ = (1 + φ)·r khi tốc độ không đổi).</summary>
    public static double BiasBound(double rate, double phi)
    {
        var a = Math.Abs(phi);
        return a >= 0.95 ? double.PositiveInfinity : a * Math.Abs(rate) / (1 - a);
    }

    /// <param name="targetKgPerWeek">Tốc độ mục tiêu có dấu (kg/tuần, giảm = âm); bị bỏ qua cho Maintenance (= 0).</param>
    /// <param name="gateReason">Kết quả <see cref="WeightTrendSeries.RateGate"/> (null = qua cổng).</param>
    public static RateClassification Classify(GoalDirection direction, double targetKgPerWeek, string? gateReason, RateUncertainty.RateEstimate? estimate, AlgorithmOptions opt)
    {
        if (gateReason is not null || estimate is null) return new(Waiting, null, null, null, null, gateReason ?? WeightTrendSeries.InsufficientWeightData);
        if (estimate.StandardError is not { } se) return new(Uncertain, null, null, null, null, NoiseNotEstimable);
        if (Math.Abs(estimate.WarmupBias) > (double)opt.RateMaxWarmupBias) return new(Uncertain, null, null, null, null, WarmupBias);

        var delta = (double)opt.RateOnTargetDeltaKgPerWeek;
        var h = (double)opt.RateOnTargetConfidenceK * (double)opt.RateSeInflation * se + BiasBound(estimate.RateKgPerWeek, estimate.WarmupBias);
        var r = estimate.RateKgPerWeek;

        if (direction == GoalDirection.Maintain)
        {
            var state = Math.Abs(r) + h <= delta ? OnTarget : r - h > delta ? Gaining : r + h < -delta ? Losing : Uncertain;
            return new(state, r, h, r - h, r + h, null);
        }

        var dev = direction == GoalDirection.Loss ? targetKgPerWeek - r : r - targetKgPerWeek;
        var s = Math.Abs(dev) + h <= delta ? OnTarget : dev - h > delta ? Faster : dev + h < -delta ? Slower : Uncertain;
        return new(s, dev, h, dev - h, dev + h, null);
    }
}
