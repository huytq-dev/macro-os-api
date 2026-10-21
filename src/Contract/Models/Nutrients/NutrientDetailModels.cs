namespace Contract;

/// <summary>
/// Một ngày của chuỗi lịch sử một nutrient (trang chi tiết nutrient): lượng đã ăn + mục tiêu CỦA NGÀY ĐÓ + dải Range.
/// </summary>
/// <param name="Amount">Lượng đã ăn trong ngày. null = chưa log ngày đó hoặc ngày có log nhưng không món nào có dữ liệu chất này (khác 0). Ngày nhịn ăn: 0 cho energy/protein/carbs/fat/fiber/sodium/net carbs, null cho chất khác.</param>
/// <param name="Floor">Cận dưới Range (chỉ vi chất; null với Calories/P/F/C).</param>
/// <param name="Target">Calories/P/F/C: target của program CỦA NGÀY ĐÓ (tuần program hiệu lực tại ngày đó); chất khác: target hiệu lực hiện tại (user ghi đè hoặc mặc định) áp cho mọi ngày. null nếu không có.</param>
/// <param name="Ceiling">Cận trên Range (chỉ vi chất).</param>
/// <param name="IsFasting">Ngày nhịn ăn (tính đã log).</param>
/// <param name="IsIncomplete">Ngày đánh dấu log chưa đủ: vẫn có Amount nhưng KHÔNG được tính vào thống kê và bucket.</param>
public sealed record NutrientSeriesDay(
    DateOnly Date,
    decimal? Amount,
    decimal? Floor,
    decimal? Target,
    decimal? Ceiling,
    bool IsFasting,
    bool IsIncomplete
);

/// <summary>Thống kê khoảng: Average = trung bình MỖI NGÀY ĐÃ LOG (có Amount, không IsIncomplete), không chia cho độ dài khoảng. Không có ngày nào → LoggedDays 0 và các giá trị null.</summary>
public sealed record NutrientSeriesSummary(
    DateOnly From,
    DateOnly To,
    int LoggedDays,
    decimal? Average,
    decimal? Min,
    decimal? Max,
    decimal? AverageTarget
);

/// <summary>Bucket Day/Week/Month: trung bình các ngày đã log hợp lệ trong bucket; chỉ có bucket chứa ít nhất một ngày như vậy.</summary>
public sealed record NutrientSeriesBucket(DateOnly Start, DateOnly End, int LoggedDays, decimal Average, decimal? AverageTarget);

/// <param name="GoalKind">Loại mục tiêu hiệu lực tại <c>To</c> (Range | Minimum | Maximum | Target | TargetWithCeiling | None, xem <c>NutrientModel.GoalKind</c>); Calories/P/F/C = Target (None khi chưa có program).</param>
/// <param name="Days">Thời gian TĂNG dần, LIÊN TỤC từ From đến To (ngày không có dữ liệu có Amount null nhưng vẫn có Target/Floor/Ceiling nếu có).</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
public sealed record NutrientSeriesResponse(
    string Code,
    string Name,
    string Unit,
    int DisplayPrecision,
    string GoalKind,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<NutrientSeriesDay> Days,
    NutrientSeriesSummary Summary,
    IReadOnlyList<NutrientSeriesBucket> Buckets
);

/// <summary>Món/recipe đóng góp vào lượng nutrient của ngày (mỗi entry một dòng).</summary>
public sealed record NutrientContributorModel(string Name, decimal Amount, Guid EntryId);

/// <summary>Lượng nutrient ăn trong khung giờ <paramref name="Hour"/> (0–23, giờ local của user).</summary>
public sealed record NutrientTimingModel(int Hour, decimal Amount);

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
    IReadOnlyList<NutrientTimingModel> Timing
);
