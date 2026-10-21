namespace Contract;

/// <summary>Số liệu cho template lý do ở FE (i18n). New* = Old* khi không có đề xuất.</summary>
public sealed record CheckInReasonParams(
    decimal AvgKcalIn,
    int DaysLogged,
    int DaysMarkedIncomplete,
    decimal? WeightChangeKg,
    int OldKcal,
    int NewKcal,
    decimal OldProteinG,
    decimal OldCarbsG,
    decimal OldFatG,
    decimal NewProteinG,
    decimal NewCarbsG,
    decimal NewFatG
);
