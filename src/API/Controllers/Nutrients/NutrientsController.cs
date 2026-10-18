namespace API;

/// <summary>Trang Nutrition: nutrient theo ngày, target floor/target/ceiling, pin lên Dashboard.</summary>
[Route("api/nutrients")]
public sealed class NutrientsController(ISender sender) : BaseApiController(sender)
{
    // --- Theo ngày ---

    /// <summary>Nutrient của một ngày (mặc định hôm nay) kèm target hiệu lực và pin; mặc định chỉ chất core, all=true thêm chất chi tiết của USDA.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DayNutrientsResponse>>> GetDay(
        [FromQuery] DateOnly? date, [FromQuery] bool all, CancellationToken ct)
        => Ok(ApiResponse<DayNutrientsResponse>.Ok(await Sender.Send(new GetDayNutrientsQuery(date, all), ct)));

    // --- Target và pin ---

    /// <summary>Danh mục nutrient kèm target hiệu lực + pin của user, không có lượng đã ăn; mặc định chỉ chất core, all=true thêm chất chi tiết.</summary>
    [HttpGet("targets")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NutrientModel>>>> GetTargets([FromQuery] bool all, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<NutrientModel>>.Ok(await Sender.Send(new GetNutrientTargetsQuery(all), ct)));

    /// <summary>Ghi đè floor/target/ceiling của một nutrient theo code; cả ba null = về mặc định.</summary>
    [HttpPut("{code}/target")]
    public async Task<ActionResult<ApiResponse<Unit>>> UpdateTarget(
        string code, [FromBody] UpdateNutrientTargetCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Code = code }, ct)));

    /// <summary>Đặt danh sách nutrient pin lên Dashboard theo thứ tự (tối đa 8), thay toàn bộ pin cũ.</summary>
    [HttpPut("pins")]
    public async Task<ActionResult<ApiResponse<Unit>>> SetPins(
        [FromBody] SetNutrientPinsCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd, ct)));
}
