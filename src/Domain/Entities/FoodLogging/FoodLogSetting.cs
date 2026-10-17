namespace Domain;

// Cài đặt trang Food Log của user, đồng bộ nhiều thiết bị. 1 dòng/user, tạo khi PUT lần đầu;
// GET chưa có dòng trả mặc định (initializer dưới đây). Model luôn ghi giá trị tường minh (không HasDefaultValue, quy ước S8).
// Chỉ ShowBrandedResults / ShowOpenFoodFactsResults có tác dụng ở BE (lọc GET /foods); còn lại FE xử lý hiển thị.
public class FoodLogSetting : EntityBase<Guid>
{
    public Guid UserId { get; set; }

    public OverageDisplay Overages { get; set; } = OverageDisplay.NoOverages;
    public int HourRangeFirst { get; set; } = 7;            // giờ đầu timeline, 0–23
    public int HourRangeLast { get; set; } = 23;            // giờ cuối timeline, 0–23, lớn hơn giờ đầu ≥ 1
    public TimelineAlignment Alignment { get; set; } = TimelineAlignment.Left;
    public AddFoodTrigger AddFoodsToHour { get; set; } = AddFoodTrigger.PlusIcon;
    public bool ShowTimestamps { get; set; } = true;
    public bool ShowHourlyTotals { get; set; } = true;
    public WeekBannerMode WeekBanner { get; set; } = WeekBannerMode.Show;
    public CalendarScrollMode CalendarScrolling { get; set; } = CalendarScrollMode.Daily;
    public bool PremoveEnabled { get; set; }
    public bool ShowBrandedResults { get; set; } = true;     // tắt → GET /foods bỏ món có brand (trừ món của chính user)
    public bool ShowOpenFoodFactsResults { get; set; } = true; // tắt → GET /foods bỏ món Source = OpenFoodFacts; barcode lookup không bị lọc
    public SearchOptimization Optimization { get; set; } = SearchOptimization.Speed;

    // Ô món trên timeline (Timeline Food Tiles): FE xử lý hiển thị, BE chỉ lưu/trả
    public TileDensity TimelineTileDensity { get; set; } = TileDensity.Standard;
    public bool TimelineShowMacros { get; set; } = true;
    public bool TimelineShowWeight { get; set; }
    public bool TimelineShowServing { get; set; } = true;
    public TileHighlight TimelineHighlight { get; set; } = TileHighlight.Edit;

    // Banner đầu bảng Thêm món (Customize Banner): FE tự tính số, BE chỉ lưu/trả
    public bool LoggerBannerShowCalories { get; set; } = true;
    public bool LoggerBannerShowProtein { get; set; }
    public bool LoggerBannerShowFat { get; set; }
    public bool LoggerBannerShowCarbs { get; set; }
    public BannerDisplay LoggerBannerDisplay { get; set; } = BannerDisplay.PlateOnly;

    // Dòng kết quả tìm món trong bảng Thêm món (Logger Food Tiles): FE xử lý hiển thị, BE chỉ lưu/trả
    public TileDensity LoggerTileDensity { get; set; } = TileDensity.Standard;
    public bool LoggerShowMacros { get; set; } = true;
    public bool LoggerShowWeight { get; set; }
    public bool LoggerShowServing { get; set; } = true;
    public SearchRowHighlight LoggerHighlight { get; set; } = SearchRowHighlight.MultiAdd;

    public User User { get; set; } = null!;
}
