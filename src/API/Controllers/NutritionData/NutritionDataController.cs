namespace API;

/// <summary>
/// Nutrition Data Manager: danh sách ngày có dữ liệu dinh dưỡng (nguồn chung của expenditure, check-in, dashboard, habits), nhập tay/sửa/xoá một ngày, đánh dấu nhịn ăn và log chưa đủ.
/// </summary>
[Route("api/nutrition-data")]
public sealed class NutritionDataController(ISender sender) : BaseApiController(sender)
{
    /// <summary>
    /// Các ngày có dữ liệu (mới nhất trước), Contract v2: dinh dưỡng HIỆU LỰC (Manual → Food Log → Fasting; null = chưa biết), `source` (Manual | FoodLog | Fasting), phần manual, tổng RAW Food Log,
    /// `isValidForAlgorithms`, `conflict`, cờ fasting/incomplete. from/to tuỳ chọn (gồm hai đầu); phân trang `limit` (mặc định 90 = kích thước trang, tối đa 366, không phải giới hạn lịch sử) và `before` (ngày cuối của trang trước = `nextBefore`, để lấy ngày cũ hơn); `hasMore`.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<NutritionDataResponse>>> Get([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? limit, [FromQuery] DateOnly? before, CancellationToken ct)
        => Ok(ApiResponse<NutritionDataResponse>.Ok(await Sender.Send(new GetNutritionDataQuery(from, to, limit, before), ct)));

    /// <summary>
    /// Lưu Manual Nutrition của một ngày (không ở tương lai theo múi giờ user): đặt override (calories/protein/fat/carbs, null = chưa nhập, 0 = nhập 0, macro-only được phép) ở 4 cột riêng của ngày; override THẮNG Food Log, không cộng,
    /// không bao giờ tạo/sửa/xoá món. Cho phép trên ngày đã có món. `isFasting`/`isIncomplete`: bỏ trống = giữ nguyên. Nhịn ăn + manual dương → 400 FASTING_WITH_MANUAL_NUTRITION; bật nhịn ăn trên ngày có entry → 409 FASTING_NEEDS_EMPTY_DAY.
    /// Xoá trống tất cả trường mà không đổi gì → 400 USE_REMOVE_OVERRIDE / NOTHING_TO_SAVE (xoá override là `DELETE {date}/override`). Lưu đúng số đang có thì không ghi gì (200).
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<NutritionDataDayModel>>> Save([FromBody] SaveNutritionDataCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<NutritionDataDayModel>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Như PUT body nhưng ngày theo route (`date` trong body bị bỏ qua).</summary>
    [HttpPut("{date}")]
    public async Task<ActionResult<ApiResponse<NutritionDataDayModel>>> SaveByDate(DateOnly date, [FromBody] SaveNutritionDataCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<NutritionDataDayModel>.Ok(await Sender.Send(cmd with { Date = date }, ct)));

    /// <summary>
    /// Delete Nutrition Data: xoá dữ liệu NDM của ngày `date` = tổng nhập tay + cờ nhịn ăn. KHÔNG xoá dòng ngày, món đã log, nước, ghi chú, cờ chưa đủ.
    /// Ngày chỉ có món Food Log (không có tổng nhập tay/nhịn ăn) → 400, không xoá gì; không có ngày → 404; ngày rỗng → không làm gì.
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete([FromQuery] DateOnly date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteNutritionDataCommand(date), ct)));

    /// <summary>Như DELETE ?date= nhưng ngày theo route.</summary>
    [HttpDelete("{date}")]
    public async Task<ActionResult<ApiResponse<Unit>>> DeleteByDate(DateOnly date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteNutritionDataCommand(date), ct)));

    // --- Thao tác tách riêng (mỗi thao tác một phạm vi) ---

    /// <summary>Remove Manual Override: chỉ gỡ tổng nhập tay của ngày; món, nước, ghi chú, nhịn ăn, chưa đủ giữ nguyên. Không có tổng nhập tay → thành công, không đổi gì; không có ngày → 404.</summary>
    [HttpDelete("{date}/override")]
    public async Task<ActionResult<ApiResponse<NutritionDataDayModel>>> RemoveOverride(DateOnly date, CancellationToken ct)
        => Ok(ApiResponse<NutritionDataDayModel>.Ok(await Sender.Send(new RemoveManualOverrideCommand(date), ct)));

    /// <summary>Mark Day Fasting: chỉ đổi cờ nhịn ăn (`isFasting`). Bật trên ngày đã có entry → 409 FASTING_NEEDS_EMPTY_DAY, không đổi gì.</summary>
    [HttpPut("{date}/fasting")]
    public async Task<ActionResult<ApiResponse<NutritionDataDayModel>>> SetFasting(DateOnly date, [FromBody] SetDayFastingCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<NutritionDataDayModel>.Ok(await Sender.Send(cmd with { Date = date }, ct)));

    /// <summary>Mark Day Incomplete: chỉ đổi cờ chưa đủ (`isIncomplete`) của ngày đã có dữ liệu; không đụng gì khác.</summary>
    [HttpPut("{date}/incomplete")]
    public async Task<ActionResult<ApiResponse<NutritionDataDayModel>>> SetIncomplete(DateOnly date, [FromBody] SetDayIncompleteCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<NutritionDataDayModel>.Ok(await Sender.Send(cmd with { Date = date }, ct)));
}
