namespace Contract;

/// <summary>
/// Cài đặt trang Food Log (enum là chuỗi). Chỉ <c>ShowBrandedResults</c> và <c>ShowOpenFoodFactsResults</c> có tác dụng ở BE (lọc GET /foods);
/// các cờ còn lại BE chỉ lưu/trả, FE xử lý hiển thị.
/// </summary>
/// <param name="Overages">NoOverages | ShowOverages (mặc định NoOverages).</param>
/// <param name="HourRangeFirst">Giờ đầu timeline 0–23 (mặc định 7).</param>
/// <param name="HourRangeLast">Giờ cuối timeline 0–23, lớn hơn giờ đầu ≥ 1 (mặc định 23).</param>
/// <param name="Alignment">Left | Right (mặc định Left).</param>
/// <param name="AddFoodsToHour">PlusIcon | LongPress (mặc định PlusIcon).</param>
/// <param name="WeekBanner">Show | ShowAndPin | Hide (mặc định Show).</param>
/// <param name="CalendarScrolling">Daily | Weekly (mặc định Daily).</param>
/// <param name="ShowBrandedResults">false → GET /foods bỏ món có thương hiệu (trừ món của chính user); mặc định true.</param>
/// <param name="ShowOpenFoodFactsResults">false → GET /foods bỏ món từ Open Food Facts; tra mã vạch không bị lọc; mặc định true.</param>
/// <param name="Optimization">Speed | Accuracy (mặc định Speed).</param>
/// <param name="TimelineTileDensity">Comfort | Standard | Compact (mặc định Standard).</param>
/// <param name="TimelineShowMacros">Hiện P/F/C trên ô món (mặc định true).</param>
/// <param name="TimelineShowWeight">Hiện khối lượng gram trên ô món (mặc định false).</param>
/// <param name="TimelineShowServing">Hiện khẩu phần trên ô món (mặc định true).</param>
/// <param name="TimelineHighlight">Edit | Energy | None (mặc định Edit).</param>
/// <param name="LoggerBannerShowCalories">Hiện kcal trên banner Thêm món (mặc định true).</param>
/// <param name="LoggerBannerShowProtein">Hiện protein trên banner (mặc định false).</param>
/// <param name="LoggerBannerShowFat">Hiện fat trên banner (mặc định false).</param>
/// <param name="LoggerBannerShowCarbs">Hiện carbs trên banner (mặc định false).</param>
/// <param name="LoggerBannerDisplay">PlateOnly | AllDayAndPlate | RemainingAfterPlate (mặc định PlateOnly); FE tự tính số.</param>
/// <param name="LoggerTileDensity">Comfort | Standard | Compact (mặc định Standard): mật độ dòng kết quả tìm món.</param>
/// <param name="LoggerShowMacros">Hiện P/F/C trên dòng kết quả tìm món (mặc định true).</param>
/// <param name="LoggerShowWeight">Hiện khối lượng gram trên dòng kết quả (mặc định false).</param>
/// <param name="LoggerShowServing">Hiện khẩu phần trên dòng kết quả (mặc định true).</param>
/// <param name="LoggerHighlight">MultiAdd | Energy | None (mặc định MultiAdd).</param>
public sealed record FoodLogSettingsModel(
    string Overages,
    int HourRangeFirst,
    int HourRangeLast,
    string Alignment,
    string AddFoodsToHour,
    bool ShowTimestamps,
    bool ShowHourlyTotals,
    string WeekBanner,
    string CalendarScrolling,
    bool PremoveEnabled,
    bool ShowBrandedResults,
    bool ShowOpenFoodFactsResults,
    string Optimization,
    string TimelineTileDensity,
    bool TimelineShowMacros,
    bool TimelineShowWeight,
    bool TimelineShowServing,
    string TimelineHighlight,
    bool LoggerBannerShowCalories,
    bool LoggerBannerShowProtein,
    bool LoggerBannerShowFat,
    bool LoggerBannerShowCarbs,
    string LoggerBannerDisplay,
    string LoggerTileDensity,
    bool LoggerShowMacros,
    bool LoggerShowWeight,
    bool LoggerShowServing,
    string LoggerHighlight
);
