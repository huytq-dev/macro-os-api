namespace API;

/// <summary>Nhật ký ăn uống: log theo ngày, thêm/sửa/xoá món, quick add, sao chép bữa hoặc cả ngày.</summary>
[Route("api/food-logs")]
public sealed class FoodLogsController(ISender sender) : BaseApiController(sender)
{
    // --- Theo ngày ---

    /// <summary>Log của một ngày (mặc định hôm nay): entries theo bữa, tổng ngày, target tuần hiệu lực.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DayLogResponse>>> GetDay(
        [FromQuery] DateOnly? date, CancellationToken ct)
        => Ok(ApiResponse<DayLogResponse>.Ok(await Sender.Send(new GetDayLogQuery(date), ct)));

    /// <summary>Tổng macro + target của 7 ngày (Thứ 2 → Chủ nhật) trong tuần chứa `date`, mặc định tuần này.</summary>
    [HttpGet("week")]
    public async Task<ActionResult<ApiResponse<WeekLogResponse>>> GetWeek(
        [FromQuery] DateOnly? date, CancellationToken ct)
        => Ok(ApiResponse<WeekLogResponse>.Ok(await Sender.Send(new GetWeekLogQuery(date), ct)));

    /// <summary>Đặt trạng thái ngày: Complete, Partial (bỏ khỏi thuật toán), Fasting (0 kcal thật, chỉ ngày trống) hoặc Unmarked.</summary>
    [HttpPut("{date}/status")]
    public async Task<ActionResult<ApiResponse<Unit>>> SetStatus(
        DateOnly date, [FromBody] SetDayStatusCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Date = date }, ct)));

    /// <summary>Xoá toàn bộ log của ngày → thuật toán coi ngày này là chưa log.</summary>
    [HttpDelete("{date}")]
    public async Task<ActionResult<ApiResponse<Unit>>> ClearDay(DateOnly date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new ClearDayLogCommand(date), ct)));

    // --- Cài đặt ---

    /// <summary>Cài đặt trang Food Log của user (đồng bộ nhiều thiết bị); chưa lưu lần nào thì trả mặc định.</summary>
    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<FoodLogSettingsModel>>> GetSettings(CancellationToken ct)
        => Ok(ApiResponse<FoodLogSettingsModel>.Ok(await Sender.Send(new GetFoodLogSettingsQuery(), ct)));

    /// <summary>Thay toàn bộ cài đặt Food Log (gửi đủ object); showBrandedResults/showOpenFoodFactsResults lọc kết quả GET /foods.</summary>
    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateSettings(
        [FromBody] UpdateFoodLogSettingsCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));

    // --- Ghi chú ---

    /// <summary>Thêm ghi chú cho một ngày (mặc định hôm nay, giờ hiện tại).</summary>
    [HttpPost("notes")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddNote(
        [FromBody] AddFoodLogNoteCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Sửa nội dung/giờ của ghi chú.</summary>
    [HttpPut("notes/{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateNote(
        Guid id, [FromBody] UpdateFoodLogNoteCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Id = id }, ct)));

    /// <summary>Xoá ghi chú.</summary>
    [HttpDelete("notes/{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> DeleteNote(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteFoodLogNoteCommand(id), ct)));

    // --- Món trong bữa ---

    /// <summary>Thêm món vào bữa; snapshot macro tại thời điểm log.</summary>
    [HttpPost("entries")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddEntry(
        [FromBody] AddFoodLogEntryCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Sửa bữa/giờ/serving/số lượng của một entry; tính lại tổng ngày.</summary>
    [HttpPut("entries/{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateEntry(
        Guid id, [FromBody] UpdateFoodLogEntryCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Id = id }, ct)));

    /// <summary>Xoá (soft) một entry; tính lại tổng ngày.</summary>
    [HttpDelete("entries/{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> DeleteEntry(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteFoodLogEntryCommand(id), ct)));

    // --- Nhập nhanh và sao chép ---

    /// <summary>Quick add: nhập thẳng kcal/P/C/F cho một bữa.</summary>
    [HttpPost("quick-add")]
    public async Task<ActionResult<ApiResponse<Guid>>> QuickAdd(
        [FromBody] QuickAddCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Copy bữa (mealType) hoặc cả ngày (mealType null) sang ngày khác; trả số entry đã copy.</summary>
    [HttpPost("copy")]
    public async Task<ActionResult<ApiResponse<int>>> Copy(
        [FromBody] CopyFoodLogCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<int>.Ok(await Sender.Send(cmd, ct)));
}
