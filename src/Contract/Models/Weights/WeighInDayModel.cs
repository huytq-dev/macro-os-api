namespace Contract;

/// <summary>Một ngày có cân và/hoặc % mỡ trong kỳ (ngày không có gì thì không có dòng: FE tô trống).</summary>
/// <param name="WeightId">Id dòng cân (null nếu ngày đó chỉ có % mỡ); xoá cân dùng <c>DELETE /api/weights/{date}</c>.</param>
/// <param name="BodyFatId">Id dòng % mỡ (xoá bằng <c>DELETE /api/body-metrics/{id}</c>); nhiều phương pháp đo trong ngày thì lấy lần ghi sau cùng.</param>
/// <param name="BodyFatMethod">Phương pháp đo của % mỡ (Visual, Scale, DEXA…).</param>
public sealed record WeighInDayModel(
    DateOnly Date,
    Guid? WeightId,
    decimal? WeightKg,
    decimal? TrendWeightKg,
    Guid? BodyFatId,
    decimal? BodyFatPercent,
    string? BodyFatMethod
);
