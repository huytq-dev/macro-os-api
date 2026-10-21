namespace Contract;

/// <summary>Một phiên ảnh (một ngày): các tư thế đã có (Front, Side, Back) kèm cân và trend của ngày đó.</summary>
public sealed record ProgressPhotoSessionModel(DateOnly Date, IReadOnlyList<ProgressPhotoModel> Photos, decimal? ScaleWeightKg, decimal? TrendWeightKg);
