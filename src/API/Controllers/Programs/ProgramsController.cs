namespace API;

[Route("api/programs")]
public sealed class ProgramsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Tạo program mới (kết thúc program cũ), sinh target tuần đầu và snapshot expenditure khởi điểm.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateProgramCommand command, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(command, ct), statusCode: 201));

    /// <summary>Lấy program đang hoạt động kèm target tuần hiện tại và lịch sử tuần.</summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<ProgramResponse>>> GetCurrent(CancellationToken ct)
        => Ok(ApiResponse<ProgramResponse>.Ok(await Sender.Send(new GetCurrentProgramQuery(), ct)));

    /// <summary>Đổi goal, rate, diet style, protein, calorie floor, ngày check-in; tính lại target tuần hiện tại.</summary>
    [HttpPut("current/goal")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateGoal([FromBody] UpdateProgramGoalCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Program")));
    }

    /// <summary>Đổi program style (Coached / Collaborative / Manual), không mất dữ liệu.</summary>
    [HttpPut("current/style")]
    public async Task<ActionResult<ApiResponse<string>>> ChangeStyle([FromBody] ChangeProgramStyleCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Program")));
    }

    /// <summary>Nhập tay TDEE khởi điểm; thuật toán expenditure học lại từ hôm nay.</summary>
    [HttpPut("current/expenditure")]
    public async Task<ActionResult<ApiResponse<string>>> SetManualExpenditure([FromBody] SetManualExpenditureCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Expenditure")));
    }

    /// <summary>Kết thúc program đang hoạt động (giữ lịch sử).</summary>
    [HttpDelete("current")]
    public async Task<ActionResult<ApiResponse<string>>> End(CancellationToken ct)
    {
        await Sender.Send(new EndProgramCommand(), ct);
        return Ok(ApiResponse<string>.OkMessage(BusinessMessages.UpdatedSuccessfully("Program")));
    }
}
