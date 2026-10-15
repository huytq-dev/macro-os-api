namespace Domain;

// Full micronutrient snapshot tại thời điểm log.
// Quan hệ 1-1 với FoodLogEntry, chỉ tạo khi FoodItem có micro data.
// Tách riêng để không bloat bảng FoodLogEntries.
public class FoodLogEntryMicro : EntityBase<Guid>
{
    public Guid FoodLogEntryId { get; set; }

    public decimal? Sugar { get; set; }           // grams
    public decimal? SaturatedFat { get; set; }    // grams
    public decimal? TransFat { get; set; }        // grams
    public decimal? Cholesterol { get; set; }     // mg
    public decimal? Potassium { get; set; }       // mg
    public decimal? VitaminA { get; set; }        // mcg
    public decimal? VitaminC { get; set; }        // mg
    public decimal? VitaminD { get; set; }        // mcg
    public decimal? Calcium { get; set; }         // mg
    public decimal? Iron { get; set; }            // mg

    public FoodLogEntry FoodLogEntry { get; set; } = null!;
}
