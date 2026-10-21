namespace Contract;

/// <summary>
/// Một ngày trong Nutrition Data Manager: tổng dinh dưỡng của ngày (nguồn dữ liệu chung của expenditure, check-in, dashboard, dải ngày) và các cờ.
/// </summary>
/// <param name="Source">App = có ít nhất một entry log từ app (món/recipe/AI…); Manual = toàn bộ do nhập tay (Quick Add) hoặc ngày nhịn ăn đánh dấu tay; (Import dành cho nguồn nhập từ ngoài, chưa dùng).</param>
/// <param name="IsFasting">Ngày nhịn ăn: tính là ngày log 0 kcal, không phá chuỗi log.</param>
/// <param name="IsIncomplete">Ngày log chưa đủ: expenditure và trung bình check-in bỏ qua ngày này (tổng ngày vẫn hiện).</param>
/// <param name="EntryCount">Số entry chưa xoá của ngày (sửa tổng của ngày có nhiều entry sẽ thay chúng bằng MỘT entry nhập tay).</param>
public sealed record NutritionDataDayModel(
    DateOnly Date,
    decimal Calories,
    decimal ProteinG,
    decimal FatG,
    decimal CarbsG,
    string Source,
    bool IsFasting,
    bool IsIncomplete,
    int EntryCount
);

/// <param name="Items">Mới nhất trước; chỉ ngày có ít nhất một entry hoặc đánh dấu nhịn ăn.</param>
public sealed record NutritionDataResponse(IReadOnlyList<NutritionDataDayModel> Items);
