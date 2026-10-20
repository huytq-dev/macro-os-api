namespace API;

/// <summary>Thư viện thực phẩm: tìm kiếm, xem chi tiết, tạo/sửa/xoá thực phẩm tự tạo của người dùng.</summary>
[Route("api/foods")]
public sealed class FoodsController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Tìm thực phẩm theo tên/brand, lọc category và phạm vi (All/Mine/System), phân trang top/skip.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<QueryResult<FoodItemModel>>>> Search(
        [FromQuery] SearchFoodsQuery query, CancellationToken ct)
        => Ok(ApiResponse<QueryResult<FoodItemModel>>.Ok(await Sender.Send(query, ct)));

    /// <summary>
    /// Tìm món chia nhóm kiểu MacroFactor: history (món đã log), common (catalog + USDA), custom (món tự tạo), branded (có thương hiệu trong DB), openFoodFacts (tìm trực tiếp ở OFF, chưa lưu DB).
    /// Không có `section`: mỗi nhóm top 5 + totalCount. Có `section` (+ `page` từ 1, `top` mặc định 20): chỉ nhóm đó, phân trang cho "See more".
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<FoodSearchResponse>>> SearchGrouped(
        [FromQuery] SearchFoodsGroupedQuery query, CancellationToken ct)
        => Ok(ApiResponse<FoodSearchResponse>.Ok(await Sender.Send(query, ct)));

    /// <summary>Chi tiết thực phẩm: dinh dưỡng /100 g và danh sách serving.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FoodItemResponse>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<FoodItemResponse>.Ok(await Sender.Send(new GetFoodQuery(id), ct)));

    /// <summary>Món hay log quanh giờ `time` (±90 phút, mặc định giờ hiện tại) và đúng thứ `weekday` trong 90 ngày gần nhất.</summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FoodSuggestionModel>>>> Suggestions(
        [FromQuery] GetFoodSuggestionsQuery query, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<FoodSuggestionModel>>.Ok(await Sender.Send(query, ct)));

    /// <summary>Mọi chất của món theo khẩu phần (servingId, quantity), kèm floor/target/ceiling hiệu lực và % so target của ngày `date`.</summary>
    [HttpGet("{id:guid}/nutrients")]
    public async Task<ActionResult<ApiResponse<FoodNutrientsResponse>>> GetNutrients(
        Guid id, [FromQuery] Guid? servingId, [FromQuery] decimal quantity = 1, [FromQuery] DateOnly? date = null, CancellationToken ct = default)
        => Ok(ApiResponse<FoodNutrientsResponse>.Ok(await Sender.Send(new GetFoodNutrientsQuery(id, servingId, quantity, date), ct)));

    /// <summary>Nhập món Foundation/SR Legacy từ USDA (theo từ khoá hoặc fdcIds); idempotent, ghi một FoodImportRun.</summary>
    [HttpPost("import/usda")]
    public async Task<ActionResult<ApiResponse<FoodImportRunModel>>> ImportUsda(
        [FromBody] ImportUsdaFoodsCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<FoodImportRunModel>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Các lần nhập dữ liệu gần nhất.</summary>
    [HttpGet("import/runs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FoodImportRunModel>>>> ImportRuns(
        [FromQuery] int top = 20, CancellationToken ct = default)
        => Ok(ApiResponse<IReadOnlyList<FoodImportRunModel>>.Ok(await Sender.Send(new GetFoodImportRunsQuery(top), ct)));

    /// <summary>Tra món theo mã vạch (8–14 chữ số): có trong DB thì trả ngay, chưa có thì lấy từ Open Food Facts và lưu lại; không thấy → 404, OFF lỗi → 502.</summary>
    [HttpGet("barcode/{code}")]
    public async Task<ActionResult<ApiResponse<FoodItemResponse>>> GetByBarcode(string code, CancellationToken ct)
        => Ok(ApiResponse<FoodItemResponse>.Ok(await Sender.Send(new GetFoodByBarcodeQuery(code), ct)));

    /// <summary>Tạo custom food của người dùng hiện tại.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateCustomFoodCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Sửa custom food do mình tạo (servings gửi lên = thay toàn bộ).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(
        Guid id, [FromBody] UpdateCustomFoodCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Id = id }, ct)));

    /// <summary>Đổi icon của món (iconKey trong bộ icon, null = bỏ): món của tôi ghi vào món, món hệ thống lưu icon riêng của user; trả iconKey hiệu lực.</summary>
    [HttpPut("{id:guid}/icon")]
    public async Task<ActionResult<ApiResponse<FoodIconResponse>>> SetIcon(
        Guid id, [FromBody] SetFoodIconCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<FoodIconResponse>.Ok(await Sender.Send(cmd with { Id = id }, ct)));

    /// <summary>Xoá (soft) custom food do mình tạo; log cũ giữ snapshot.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteCustomFoodCommand(id), ct)));
}
