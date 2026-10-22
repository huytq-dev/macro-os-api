namespace API;

/// <summary>Expenditure (TDEE ước lượng): chuỗi theo ngày, tổng hợp tiến độ ngày, reset học lại.</summary>
[Route("api/expenditure")]
public sealed class ExpenditureController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Chuỗi TDEE ước lượng theo ngày (mặc định 90 ngày), status hiện tại và lý do Holding.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ExpenditureResponse>>> GetSeries(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<ExpenditureResponse>.Ok(await Sender.Send(new GetExpenditureQuery(from, to), ct)));

    /// <summary>Trang Expenditure: TDEE mỗi ngày liên tục đến hôm nay (ngày chưa có snapshot giữ TDEE gần nhất) + Flux Range + thay đổi/ngày + thống kê + bucket (from mặc định 90 ngày, all=true từ ngày đầu).</summary>
    [HttpGet("series")]
    public async Task<ActionResult<ApiResponse<ExpenditureSeriesResponse>>> GetDailySeries(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] bool? all, [FromQuery] MetricGranularity? granularity, CancellationToken ct)
        => Ok(ApiResponse<ExpenditureSeriesResponse>.Ok(await Sender.Send(new GetExpenditureSeriesQuery(from, to, all, granularity), ct)));

    /// <summary>Insights &amp; Data: TDEE hôm nay, strategy (Calibrating | Updating | Holding) + lý do Holding, thay đổi 3/7/14/30/90 ngày (Decrease | Increase | NoChange | Waiting).</summary>
    [HttpGet("insights")]
    public async Task<ActionResult<ApiResponse<ExpenditureInsightsResponse>>> GetInsights(CancellationToken ct)
        => Ok(ApiResponse<ExpenditureInsightsResponse>.Ok(await Sender.Send(new GetExpenditureInsightsQuery(), ct)));

    /// <summary>Biến thiên TDEE so với 3/7/14/30/90 ngày trước; Waiting khi chưa đủ dữ liệu ở mốc đó.</summary>
    [HttpGet("changes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExpenditureChangeModel>>>> GetChanges(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<ExpenditureChangeModel>>.Ok(await Sender.Send(new GetExpenditureChangesQuery(), ct)));

    /// <summary>Tổng hợp ngày (kcal/macro ăn vs target, trend) cho Energy Balance / Habits (mặc định 30 ngày).</summary>
    [HttpGet("daily")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DailyProgressModel>>>> GetDaily(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<DailyProgressModel>>.Ok(await Sender.Send(new GetDailyProgressQuery(from, to), ct)));

    // --- Settings (Expenditure Settings; chỉ hoạt động sau khi migration S25 được duyệt và áp) ---

    /// <summary>Cài đặt Expenditure: Initial Estimate, BMR equation, Athletic Pursuits, Prior Weight (+ cân lúc trả lời), Start Date, chế độ Dynamic/Static hôm nay.</summary>
    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<ExpenditureSettingsResponse>>> GetSettings(CancellationToken ct)
        => Ok(ApiResponse<ExpenditureSettingsResponse>.Ok(await Sender.Send(new GetExpenditureSettingsQuery(), ct)));

    /// <summary>Cập nhật từng phần cài đặt ước tính ban đầu (không reset/đổi expenditure đã học).</summary>
    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<ExpenditureSettingsResponse>>> UpdateSettings([FromBody] UpdateExpenditureSettingsCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<ExpenditureSettingsResponse>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Chuyển Dynamic ↔ Static từ hôm nay (Static cần giá trị TDEE); engine vẫn học ngầm, không reset.</summary>
    [HttpPost("mode")]
    public async Task<ActionResult<ApiResponse<ExpenditureSettingsResponse>>> SetMode([FromBody] SetExpenditureModeCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<ExpenditureSettingsResponse>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Reset learning: thuật toán bỏ qua dữ liệu trước hôm nay, giữ TDEE hiện tại làm khởi điểm.</summary>
    [HttpPost("reset")]
    public async Task<ActionResult<ApiResponse<Unit>>> Reset(CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new ResetExpenditureCommand(), ct)));
}
