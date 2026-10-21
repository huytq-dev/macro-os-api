namespace API;

/// <summary>
/// Nutrition Data Manager: danh sách ngày có dữ liệu dinh dưỡng (nguồn chung của expenditure, check-in, dashboard, habits), nhập tay/sửa/xoá một ngày, đánh dấu nhịn ăn và log chưa đủ.
/// </summary>
[Route("api/nutrition-data")]
public sealed class NutritionDataController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Các ngày có dữ liệu (mới nhất trước) kèm kcal/macros của ngày, nguồn (App | Manual) và cờ fasting/incomplete; from/to tuỳ chọn (gồm hai đầu).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<NutritionDataResponse>>> Get([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(ApiResponse<NutritionDataResponse>.Ok(await Sender.Send(new GetNutritionDataQuery(from, to), ct)));

    /// <summary>
    /// Lưu một ngày (không ở tương lai theo múi giờ user): `isFasting` = ngày nhịn ăn (tính 0 kcal, không phá chuỗi); có `calories` = tạo/cập nhật đúng MỘT entry nhập tay;
    /// không có `calories` thì chỉ đổi `isIncomplete` của ngày đã có dữ liệu. Ngày có món từ Food Log (source App): chỉ đổi được `isIncomplete`, gửi calories/isFasting → 400. `isIncomplete` = expenditure bỏ qua ngày này.
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<NutritionDataDayModel>>> Save([FromBody] SaveNutritionDataCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<NutritionDataDayModel>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Như PUT body nhưng ngày theo route (`date` trong body bị bỏ qua).</summary>
    [HttpPut("{date}")]
    public async Task<ActionResult<ApiResponse<NutritionDataDayModel>>> SaveByDate(DateOnly date, [FromBody] SaveNutritionDataCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<NutritionDataDayModel>.Ok(await Sender.Send(cmd with { Date = date }, ct)));

    /// <summary>Xoá cả ngày `date` nhập tay / nhịn ăn (thuật toán coi là chưa log); ngày có món từ Food Log (source App) → 400, không xoá gì; không có ngày → 404.</summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete([FromQuery] DateOnly date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteNutritionDataCommand(date), ct)));

    /// <summary>Như DELETE ?date= nhưng ngày theo route.</summary>
    [HttpDelete("{date}")]
    public async Task<ActionResult<ApiResponse<Unit>>> DeleteByDate(DateOnly date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteNutritionDataCommand(date), ct)));
}
