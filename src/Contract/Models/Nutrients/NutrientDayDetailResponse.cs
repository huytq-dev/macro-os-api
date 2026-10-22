namespace Contract;

/// <summary>Trang chi tiết nutrient cho MỘT ngày: Today / Past Week / Past Month so với target, Active Goal, All Contributors, Nutrient Timing.</summary>
/// <param name="Amount">Lượng ngày <c>Date</c> (cùng quy tắc <c>GET /nutrients</c>: ngày chưa log = 0 cho macro/fiber/sodium, null cho vi chất).</param>
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
/// <param name="Contributors">TẤT CẢ entry của ngày có đóng góp &gt; 0, lượng giảm dần (FE lấy top 3 cho "Top 3 Contributors"). Nước uống từ WaterLog không có entry nên không nằm ở đây.</param>
/// <param name="Timing">Luôn 24 phần tử (giờ 0–23) theo giờ ăn của entry (EatenAt; thiếu giờ thì giờ mặc định của bữa như day log); tất cả 0 khi chưa có dữ liệu.</param>
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
    string? TargetProvenance = null
);
// TargetProvenance: chỉ Calories/P/F/C (target từ program tại Date): VERIFIED | LEGACY_UNVERIFIED (Target chỉ để tham khảo, "Chưa xác minh", không badge đạt/không đạt); null = chất khác hoặc chưa có target.
