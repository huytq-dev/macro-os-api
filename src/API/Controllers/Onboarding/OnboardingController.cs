namespace API;

/// <summary>Wizard thiết lập sau đăng nhập: Basics → Goal → Program → đồng ý điều khoản.</summary>
[Route("api/onboarding")]
public sealed class OnboardingController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Trạng thái wizard, bước đang dở và đáp án đã lưu; `isRequired` = chưa có program hoặc chưa đồng ý điều khoản.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<OnboardingStateResponse>>> GetState(CancellationToken ct)
        => Ok(ApiResponse<OnboardingStateResponse>.Ok(await Sender.Send(new GetOnboardingStateQuery(), ct)));

    /// <summary>Lưu đáp án Basics (một phần) và cân nặng hôm nay; cập nhật bước hiện tại. Không ghi đè lần cân hôm nay đã có: `weightKept` = true khi số user nhập khác số đã lưu (FE hiện thông báo ngắn).</summary>
    [HttpPut("basics")]
    public async Task<ActionResult<ApiResponse<SaveOnboardingBasicsResponse>>> SaveBasics(
        [FromBody] SaveOnboardingBasicsCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<SaveOnboardingBasicsResponse>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Số "X" của câu hỏi Prior Weight ("Have you ever weighed more than X?"): chính xác + số hiển thị (làm tròn lên theo đơn vị). Công thức tạm MACROOS_PROVISIONAL_POLICY (cân/0.9), chưa phải công thức MF đã xác minh.</summary>
    [HttpGet("prior-weight-question")]
    public async Task<ActionResult<ApiResponse<PriorWeightQuestionResponse>>> GetPriorWeightQuestion(
        [FromQuery] decimal? weightKg, [FromQuery] string? unit, CancellationToken ct)
        => Ok(ApiResponse<PriorWeightQuestionResponse>.Ok(await Sender.Send(new GetPriorWeightQuestionQuery(weightKg, unit), ct)));

    /// <summary>TDEE khởi điểm ước tính từ đáp án Basics đã lưu.</summary>
    [HttpGet("estimate")]
    public async Task<ActionResult<ApiResponse<TdeeEstimateResponse>>> GetEstimate(CancellationToken ct)
        => Ok(ApiResponse<TdeeEstimateResponse>.Ok(await Sender.Send(new GetTdeeEstimateQuery(), ct)));

    /// <summary>Xem trước goal/program (kcal, ngày dự kiến, macro 7 ngày) mà không ghi dữ liệu.</summary>
    [HttpPost("preview")]
    [SkipDerivedDataState]
    public async Task<ActionResult<ApiResponse<ProgramPreviewResponse>>> Preview(
        [FromBody] PreviewProgramQuery query, CancellationToken ct)
        => Ok(ApiResponse<ProgramPreviewResponse>.Ok(await Sender.Send(query, ct)));

    /// <summary>Hoàn tất thiết lập sau khi đã tạo program và đồng ý hai điều khoản.</summary>
    [HttpPost("complete")]
    public async Task<ActionResult<ApiResponse<Unit>>> Complete(
        [FromBody] CompleteOnboardingCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));
}
