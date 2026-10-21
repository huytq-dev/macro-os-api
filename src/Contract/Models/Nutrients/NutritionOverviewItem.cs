namespace Contract;

/// <param name="Key">Khoá camelCase theo danh mục FE (calories, protein, …, netCarbs, b12, vitaminC, omega3Ala, …).</param>
/// <param name="Value">range=today: tổng trong ngày; range khác: trung bình mỗi ngày đã log trong kỳ. null = chưa có dữ liệu chất đó (FE hiện "—", khác 0; netCarbs null khi chưa biết fiber).</param>
/// <param name="Target">Mục tiêu hiệu lực (user ghi đè, thiếu thì mặc định; Calories/P/F/C từ program, kỳ dài = trung bình target các ngày đã log); null nếu chất không có target (vd Net, Starch).</param>
/// <param name="Floor">Mức tối thiểu hiệu lực (DRI/user); null = không có. FE vẽ vùng mục tiêu giữa Floor và Ceiling quanh Target.</param>
/// <param name="Ceiling">Trần hiệu lực (UL áp cho thực phẩm, DGA/FDA/CDRR hoặc user); null = không có.</param>
/// <param name="Unit">Đơn vị của chất theo danh mục (kcal, g, mg, mcg): FE không hardcode.</param>
/// <param name="GoalKind">Range | Minimum | Maximum | Target | TargetWithCeiling | None (xem <c>NutrientModel.GoalKind</c>) từ Floor/Target/Ceiling của item; kỳ dài Calories/P/F/C dùng target trung bình các ngày đã log (Target), vi chất dùng mức hiệu lực hiện tại như chuỗi /nutrients/{code}/series.</param>
public sealed record NutritionOverviewItem(string Key, decimal? Value, decimal? Target, decimal? Floor = null, decimal? Ceiling = null, string? Unit = null, string GoalKind = "None");
