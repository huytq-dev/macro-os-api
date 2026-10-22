namespace Contract;

/// <summary>Xem trước goal/program trước khi tạo: ngân sách kcal, ngày dự kiến đạt mục tiêu, macro từng ngày.</summary>
/// <param name="WeeklyChangeKg">Có dấu: dương = tăng cân, âm = giảm.</param>
/// <param name="ProjectedEndDate">Ước lượng lạc quan; null khi giữ cân, chưa chọn cân mục tiêu hoặc mục tiêu ngược hướng.</param>
public sealed record ProgramPreviewResponse(
    int TdeeKcal,
    int AverageCalories,
    decimal ProteinGPerKg,
    decimal WeeklyChangeKg,
    decimal WeeklyChangePercent,
    DateOnly? ProjectedEndDate,
    int FloorStandardKcal,
    int FloorLowKcal,
    decimal CurrentWeightKg,
    IReadOnlyList<ProgramPreviewDay> Days,
    FeasibilityResponse? Feasibility = null
);
