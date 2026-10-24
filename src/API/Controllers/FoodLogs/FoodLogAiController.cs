namespace API;

/// <summary>Food Log: gợi ý món ăn bằng AI từ ảnh/mô tả (tab AI của bảng Thêm món).</summary>
[Route("api/food-logs/ai")]
public sealed class FoodLogAiController(ISender sender) : BaseApiController(sender)
{
    // 3 ảnh × 5 MB + phần form; chặn sớm ở Kestrel trước khi đọc vào bộ nhớ
    private const long MaxBodyBytes = 18_000_000;

    /// <summary>Pha 1: nhận diện món + nguyên liệu + gram từ tối đa 3 ảnh (photos) và/hoặc mô tả (description); không có dinh dưỡng, không ghi nhật ký.</summary>
    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<ActionResult<ApiResponse<AiAnalysisResponse>>> Analyze(
        [FromForm] List<IFormFile>? photos, [FromForm] string? description, CancellationToken ct)
    {
        var list = new List<MealPhoto>();
        foreach (var file in photos ?? [])
        {
            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            list.Add(new MealPhoto(buffer.ToArray()));
        }
        return Ok(ApiResponse<AiAnalysisResponse>.Ok(await Sender.Send(new AnalyzeMealCommand(list, description), ct)));
    }

    /// <summary>Pha 2: tìm một nguyên liệu (name + grams) trong DB thực phẩm, macro tính cho đúng khối lượng trả về; không khớp thì AI ước lượng (estimated=true) hoặc 0.</summary>
    [HttpPost("resolve-ingredient")]
    [SkipDerivedDataState]
    public async Task<ActionResult<ApiResponse<AiResolvedIngredientModel>>> ResolveIngredient(
        [FromBody] ResolveAiIngredientQuery query, CancellationToken ct)
        => Ok(ApiResponse<AiResolvedIngredientModel>.Ok(await Sender.Send(query, ct)));

    /// <summary>Xác nhận kết quả phân tích khi user log: scan → Confirmed và nhớ lựa chọn nguyên liệu (món/canonical) cho lần resolve sau.</summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<ApiResponse<AiScanConfirmResponse>>> Confirm(
        [FromBody] ConfirmAiScanCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<AiScanConfirmResponse>.Ok(await Sender.Send(cmd, ct)));
}
