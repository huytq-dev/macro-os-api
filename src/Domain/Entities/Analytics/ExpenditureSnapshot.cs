namespace Domain;

// "Não" của hệ thống: lưu TDEE estimate được tính theo thuật toán CICO ngược
// (CaloriesOut = CaloriesIn - ΔStoredEnergy)
// Được tạo/cập nhật mỗi khi có đủ dữ liệu trong window
public class ExpenditureSnapshot : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly SnapshotDate { get; set; }

    // TDEE estimate (kcal/day)
    public decimal EstimatedTdeeKcal { get; set; }

    // Trạng thái của algorithm
    public ExpenditureStatus Status { get; set; }

    // Input data của window tính toán
    public decimal AvgCaloriesConsumed { get; set; }    // avg thực tế trong window
    public decimal? WeightChangeRateKgPerWeek { get; set; } // từ trend weight
    public int WindowDays { get; set; }                  // số ngày trong window (thường 20)
    public string AlgorithmVersion { get; set; } = "1";
    public int? CalculationRevision { get; set; }           // UserExpenditureSettings.CalculationRevision lúc dòng này được tính; null = trước khi có revision (Legacy)
    public string? ReasonCodesJson { get; set; }        // vd ["FEW_LOGGED_DAYS"]
    public decimal? QualityScore { get; set; }              // 0..1
    public decimal? Completeness { get; set; }              // tỉ lệ ngày hợp lệ trong window
    public DateOnly? WindowStartDate { get; set; }
    public DateOnly? WindowEndDate { get; set; }

    public User User { get; set; } = null!;
}
