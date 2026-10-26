// Domain/Entities/Nutrition/FoodLog.cs
// 1 FoodLog = 1 ngày của 1 user
using Domain;

public class FoodLog : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public string? Notes { get; set; }

    // User đánh dấu ngày log chưa đủ (check-in Partial Logging): expenditure bỏ qua ngày này; tổng ngày vẫn recalc bình thường, cờ không bị ghi đè
    public bool IsIncomplete { get; set; }

    // User đánh dấu ngày nhịn ăn (Nutrition Data Manager): ngày không có entry nhưng TÍNH là đã log với 0 kcal (expenditure, check-in, dải ngày); thêm entry vào ngày thì cờ tự tắt
    public bool IsFasting { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    // Manual Nutrition (S29): tổng dinh dưỡng CỦA CẢ NGÀY nhập qua Edit Day / Nutrition Data Manager; ghi đè Food Log (override wins), KHÔNG cộng với Food Log và KHÔNG phải FoodLogEntry.
    // null = chưa nhập (không phải 0); 0 = người dùng chủ động nhập 0. Override tồn tại ⇔ có ít nhất một cột khác null. Món ăn không bao giờ ghi các cột này.
    public decimal? ManualCaloriesKcal { get; set; }
    public decimal? ManualProteinG { get; set; }
    public decimal? ManualCarbsG { get; set; }
    public decimal? ManualFatG { get; set; }

    // Denormalized daily totals (recalculated on entry change): tổng RAW của các món Food Log, không tính manual
    public decimal TotalCalories { get; set; }
    public decimal TotalProtein { get; set; }
    public decimal TotalCarbs { get; set; }
    public decimal TotalFat { get; set; }
    public decimal TotalFiber { get; set; }
    public decimal TotalWater { get; set; }     // ml

    public User User { get; set; } = null!;
    public ICollection<FoodLogEntry> Entries { get; set; } = [];
    public ICollection<WaterLog> WaterLogs { get; set; } = [];
}