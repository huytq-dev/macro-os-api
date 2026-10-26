namespace Contract;

/// <summary>
/// Bucket Week/Month của chuỗi expenditure: <see cref="MetricBucket"/> + dải Flux của bucket.
/// CHÍNH SÁCH GỘP DẢI (MACROOS, MF không công bố): dải bucket = <c>Average</c> ± NỬA ĐỘ RỘNG trung bình của các ngày CÓ DẢI trong bucket
/// (<c>FluxLowKcal = ⌊Average − mean(kcal − Lower)⌋</c>, <c>FluxHighKcal = ⌈Average + mean(Upper − kcal)⌉</c>). Luôn <c>FluxLowKcal ≤ Average ≤ FluxHighKcal</c> khi có dải (kể cả bucket có cả ngày Calibrating không dải);
/// bề rộng cùng cỡ dải ngày, không phình theo độ dài bucket; bucket không ngày nào có dải ⇒ null.
/// </summary>
public sealed record ExpenditureBucketModel(DateOnly Start, DateOnly End, int Count, decimal Average, decimal? FluxLowKcal, decimal? FluxHighKcal);
