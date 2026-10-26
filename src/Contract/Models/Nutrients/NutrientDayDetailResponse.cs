namespace Contract;

/// <summary>Trang chi tiết nutrient cho MỘT ngày: Today / Past Week / Past Month so với target, Active Goal, All Contributors, Nutrient Timing.</summary>
/// <param name="Amount">Lượng ngày <c>Date</c> (cùng quy tắc <c>GET /nutrients</c>: ngày chưa log = 0 cho macro/fiber/sodium, null cho vi chất). Calories/P/F/C/Net carbs theo Effective Daily Nutrition: ngày có manual override thì Amount = đúng giá trị manual của trường đó, null khi trường đó chưa nhập (không lấy từ món, không 0); Net carbs null khi có override (fiber chỉ từ món).</param>
/// <param name="Floor">Mức hiệu lực (cận dưới); null nếu không có.</param>
/// <param name="Target">Mức hiệu lực: Calories/P/F/C = target của program tại <c>Date</c>.</param>
/// <param name="Ceiling">Cận trên hiệu lực; null nếu không có.</param>
/// <param name="GoalKind">Range | Minimum | Maximum | Target | TargetWithCeiling | None (xem <c>NutrientModel.GoalKind</c>) từ Floor/Target/Ceiling hiệu lực của ngày.</param>
/// <param name="TargetSource">Strategy (Calories/P/F/C: đặt theo program) | Custom (user ghi đè) | Default (DRI/mặc định) | None (chất không có mức nào).</param>
/// <param name="TargetBasis">Chi tiết nguồn: program, user, dri, reference, default, none (cùng <c>NutrientModel.TargetBasis</c>).</param>
/// <param name="PastWeekAverage">Trung bình mỗi ngày đã log (không IsIncomplete) trong 7 ngày kết thúc ở <c>Date</c> (gồm Date); null khi không có ngày nào.</param>
/// <param name="PastWeekLoggedDays">Số ngày đã dùng.</param>
/// <param name="PastMonthAverage">Như tuần nhưng 30 ngày kết thúc ở <c>Date</c>.</param>
/// <param name="PastMonthLoggedDays">Số ngày đã dùng.</param>
/// <param name="Contributors">TẤT CẢ entry của ngày có đóng góp &gt; 0, lượng giảm dần (FE lấy top 3 cho "Top 3 Contributors"). Nước uống từ WaterLog không có entry nên không nằm ở đây. RỖNG cho Calories/P/F/C/Net carbs khi ngày có manual override (Amount là số manual, các món không được tính nên không liệt kê như đóng góp); đọc <c>AmountSource</c> để phân biệt với "chưa có món".</param>
/// <param name="Timing">Luôn 24 phần tử (giờ 0–23) theo giờ ăn của entry (EatenAt; thiếu giờ thì giờ mặc định của bữa như day log); tất cả 0 khi chưa có dữ liệu, và tất cả 0 cho Calories/P/F/C/Net carbs khi ngày có manual override (manual không có giờ ăn).</param>
public sealed record NutrientDayDetailResponse(
    string Code,
    string Name,
    string Unit,
    int DisplayPrecision,
    DateOnly Date,
    decimal? Amount,
    decimal? Floor,
    decimal? Target,
    decimal? Ceiling,
    string GoalKind,
    string TargetSource,
    string TargetBasis,
    decimal? PastWeekAverage,
    int PastWeekLoggedDays,
    decimal? PastMonthAverage,
    int PastMonthLoggedDays,
    IReadOnlyList<NutrientContributorModel> Contributors,
    IReadOnlyList<NutrientTimingModel> Timing,
    string? TargetProvenance = null,
    string? AmountSource = null
);
// AmountSource: chỉ Calories/P/F/C/Net carbs (nguồn của Amount theo Effective Daily Nutrition): Manual (ngày có override) | FoodLog | Fasting (nhịn ăn hợp lệ, 0); null = chất khác (luôn từ món).
// TargetProvenance: chỉ Calories/P/F/C (target từ program tại Date): VERIFIED | LEGACY_UNVERIFIED (Target chỉ để tham khảo, "Chưa xác minh", không badge đạt/không đạt); null = chất khác hoặc chưa có target.
