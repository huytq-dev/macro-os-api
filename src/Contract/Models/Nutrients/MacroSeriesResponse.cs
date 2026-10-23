namespace Contract;

/// <summary>Biểu đồ Macros (Protein / Fat / Carbs / Other kcal): <c>Days</c> LIÊN TỤC tăng dần từ From đến To (To ≤ hôm nay local), <c>Summary</c> của cả khoảng, <c>Buckets</c> rỗng khi không gửi granularity.</summary>
public sealed record MacroSeriesResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<MacroSeriesDay> Days,
    MacroSeriesSummary Summary,
    IReadOnlyList<MacroSeriesBucket> Buckets
);
