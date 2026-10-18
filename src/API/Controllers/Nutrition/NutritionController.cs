namespace API;

/// <summary>Tổng quan dinh dưỡng theo kỳ cho trang Nutrition Overview.</summary>
[Route("api/nutrition")]
public sealed class NutritionController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Mọi chất theo kỳ (today/week/month/quarter/year, kết thúc ở date): today = tổng ngày + top 3 món đóng góp; kỳ khác = trung bình mỗi ngày đã log; kèm target.</summary>
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<NutritionOverviewResponse>>> GetOverview(
        [FromQuery] GetNutritionOverviewQuery query, CancellationToken ct)
        => Ok(ApiResponse<NutritionOverviewResponse>.Ok(await Sender.Send(query, ct)));

    /// <summary>Dựng lại tổng dinh dưỡng theo ngày (mặc định 365 ngày gần nhất) — backfill; trả số ngày có dữ liệu.</summary>
    [HttpPost("aggregates/rebuild")]
    public async Task<ActionResult<ApiResponse<int>>> RebuildAggregates(
        [FromBody] RebuildNutritionAggregatesCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<int>.Ok(await Sender.Send(cmd, ct)));
}
