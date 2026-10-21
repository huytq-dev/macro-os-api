namespace API;

/// <summary>Energy Balance: kcal ăn so với expenditure và với target của từng ngày (hai tab Expenditure / Calorie Targets).</summary>
[Route("api/energy-balance")]
public sealed class EnergyBalanceController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Chuỗi ngày liên tục tới hôm nay: calories, expenditure, target, chênh lệch + thống kê (trung bình mỗi ngày đã log) + bucket (from mặc định 90 ngày, all=true từ ngày có dữ liệu đầu).</summary>
    [HttpGet("series")]
    public async Task<ActionResult<ApiResponse<EnergyBalanceSeriesResponse>>> GetSeries(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] bool? all, [FromQuery] MetricGranularity? granularity, CancellationToken ct)
        => Ok(ApiResponse<EnergyBalanceSeriesResponse>.Ok(await Sender.Send(new GetEnergyBalanceSeriesQuery(from, to, all, granularity), ct)));

    /// <summary>Insights &amp; Data: Relative to Expenditure / Relative to Targets cho 3/7/14/30/90 ngày (trung bình mỗi ngày đã log, nhãn Deficit/Surplus, BelowTarget/AboveTarget, Waiting).</summary>
    [HttpGet("insights")]
    public async Task<ActionResult<ApiResponse<EnergyBalanceInsightsResponse>>> GetInsights(CancellationToken ct)
        => Ok(ApiResponse<EnergyBalanceInsightsResponse>.Ok(await Sender.Send(new GetEnergyBalanceInsightsQuery(), ct)));
}
