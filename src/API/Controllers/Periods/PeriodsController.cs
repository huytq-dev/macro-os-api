namespace API;

/// <summary>Chu kỳ kinh: log, sửa, xoá; độ dài chu kỳ tự tính lại.</summary>
[Route("api/periods")]
public sealed class PeriodsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Các kỳ kinh (mới nhất trước), mặc định 365 ngày (tối đa 3660); kèm kỳ gần nhất, số ngày kể từ đó, độ dài kỳ gần nhất và `days` = các ngày đã theo dõi trong khoảng.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PeriodLogsResponse>>> GetList(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<PeriodLogsResponse>.Ok(await Sender.Send(new GetPeriodLogsQuery(from, to), ct)));

    /// <summary>Bật/tắt MỘT ngày kinh (sheet "Are you on your period?"): `isOnPeriod` true → kéo dài / gộp / tạo kỳ, false → thu hẹp / tách / xoá kỳ; idempotent, một transaction; ngày tương lai 400.</summary>
    [HttpPut("days/{date}")]
    public async Task<ActionResult<ApiResponse<Unit>>> SetDay(
        DateOnly date, [FromBody] SetPeriodDayCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Date = date }, ct)));

    /// <summary>Tắt ngày kinh (nút thùng rác của sheet): như PUT days/{date} với isOnPeriod = false; ngày chưa theo dõi thì không đổi.</summary>
    [HttpDelete("days/{date}")]
    public async Task<ActionResult<ApiResponse<Unit>>> ClearDay(DateOnly date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new SetPeriodDayCommand(date, false), ct)));

    /// <summary>Thêm kỳ kinh; EndDate null = đang trong kỳ.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreatePeriodLogCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Sửa kỳ kinh (đổi ngày, đóng kỳ, triệu chứng/ghi chú); tính lại độ dài chu kỳ.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(
        Guid id, [FromBody] UpdatePeriodLogCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Id = id }, ct)));

    /// <summary>Xoá kỳ kinh; tính lại độ dài chu kỳ các kỳ còn lại.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeletePeriodLogCommand(id), ct)));
}
