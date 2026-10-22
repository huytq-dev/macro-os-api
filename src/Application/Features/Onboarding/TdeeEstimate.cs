namespace Application;

public sealed record TdeeEstimate(int TdeeKcal, int BmrKcal, string Method, decimal? BodyFatPercent = null);
