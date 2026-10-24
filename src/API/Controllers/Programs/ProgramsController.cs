namespace API;

/// <summary>Program dinh dưỡng của người dùng: tạo, xem program hiện tại và target tuần, đổi mục tiêu/style/TDEE, kết thúc.</summary>
[Route("api/programs")]
public sealed class ProgramsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Tạo program mới (kết thúc program cũ), sinh target tuần đầu và snapshot expenditure khởi điểm.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateProgramCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Số liệu wizard New Program: cân nặng, TDEE, sàn kcal chuẩn/thấp, trần kcal/protein, 7 ngày mặc định.</summary>
    [HttpGet("setup")]
    public async Task<ActionResult<ApiResponse<ProgramSetupResponse>>> GetSetup(CancellationToken ct)
        => Ok(ApiResponse<ProgramSetupResponse>.Ok(await Sender.Send(new GetProgramSetupQuery(), ct)));

    /// <summary>Xem trước program từ prefs của wizard (cùng body với tạo program), không ghi DB.</summary>
    [HttpPost("preview")]
    [SkipDerivedDataState]
    public async Task<ActionResult<ApiResponse<ProgramDraftPreviewResponse>>> Preview(
        [FromBody] CreateProgramCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<ProgramDraftPreviewResponse>.Ok(await Sender.Send(new PreviewNewProgramQuery(cmd), ct)));

    /// <summary>Lấy program đang hoạt động kèm target tuần hiện tại và lịch sử tuần.</summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<ProgramResponse>>> GetCurrent(CancellationToken ct)
        => Ok(ApiResponse<ProgramResponse>.Ok(await Sender.Send(new GetCurrentProgramQuery(), ct)));

    /// <summary>Đổi goal, rate, diet style, protein, calorie floor, ngày check-in; tính lại target tuần hiện tại.</summary>
    [HttpPut("current/goal")]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateGoal(
        [FromBody] UpdateProgramGoalCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Đổi program style (Coached / Collaborative / Manual), không mất dữ liệu.</summary>
    [HttpPut("current/style")]
    public async Task<ActionResult<ApiResponse<Unit>>> ChangeStyle(
        [FromBody] ChangeProgramStyleCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Nhập tay TDEE khởi điểm; thuật toán expenditure học lại từ hôm nay.</summary>
    [HttpPut("current/expenditure")]
    public async Task<ActionResult<ApiResponse<Unit>>> SetManualExpenditure(
        [FromBody] SetManualExpenditureCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Kết thúc program đang hoạt động (giữ lịch sử).</summary>
    [HttpDelete("current")]
    public async Task<ActionResult<ApiResponse<Unit>>> End(CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new EndProgramCommand(), ct)));
}
