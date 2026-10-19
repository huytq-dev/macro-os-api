namespace API;

/// <summary>
/// Form multipart của PUT /api/progress-photos. Gom thành MỘT model vì Swashbuckle không sinh được operation khi trộn <c>[FromForm] IFormFile</c>
/// với các <c>[FromForm]</c> scalar ở tham số riêng (ném SwaggerGeneratorException làm /swagger/v1/swagger.json trả 500).
/// </summary>
public sealed class UpsertProgressPhotoForm
{
    /// <summary>Ảnh JPEG/PNG/WebP ≤ 10 MB.</summary>
    public IFormFile? File { get; set; }
    /// <summary>Front | Side | Back.</summary>
    public PhotoAngle Angle { get; set; }
    /// <summary>Ngày của ảnh (yyyy-MM-dd); bỏ trống = hôm nay.</summary>
    public DateOnly? Date { get; set; }
    /// <summary>Đồng ý lưu ảnh riêng tư; bắt buộc true ở lần tải đầu tiên.</summary>
    public bool Consent { get; set; }
}

/// <summary>Ảnh tiến độ (Front/Side/Back theo ngày): dữ liệu nhạy cảm, lưu ở kho riêng tư, chỉ chủ sở hữu xem được qua URL do BE ký (hết hạn ngắn).</summary>
[Route("api/progress-photos")]
public sealed class ProgressPhotosController(ISender sender) : BaseApiController(sender)
{
    // 10 MB ảnh + phần form; chặn sớm ở Kestrel trước khi đọc vào bộ nhớ
    private const long MaxBodyBytes = 11_534_336;

    /// <summary>Ảnh của một ngày (mặc định hôm nay) kèm URL xem có chữ ký, hết hạn sau vài phút, và cờ hasConsent.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProgressPhotosResponse>>> Get([FromQuery] DateOnly? date, CancellationToken ct)
        => Ok(ApiResponse<ProgressPhotosResponse>.Ok(await Sender.Send(new GetProgressPhotosQuery(date), ct)));

    /// <summary>Lưu hoặc thay ảnh của (ngày, góc): multipart gồm file (JPEG/PNG/WebP ≤ 10 MB), angle, date, consent (bắt buộc true ở lần đầu).</summary>
    [HttpPut]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<ActionResult<ApiResponse<ProgressPhotoModel>>> Upsert([FromForm] UpsertProgressPhotoForm form, CancellationToken ct)
    {
        // Chỉ đổi IFormFile → byte[] (Application không tham chiếu ASP.NET); thiếu file → validator báo Data bắt buộc
        using var buffer = new MemoryStream();
        if (form.File is { Length: > 0 })
        {
            await using var stream = form.File.OpenReadStream();
            await stream.CopyToAsync(buffer, ct);
        }
        return Ok(ApiResponse<ProgressPhotoModel>.Ok(await Sender.Send(new UpsertProgressPhotoCommand(form.Date, form.Angle, buffer.ToArray(), form.Consent), ct)));
    }

    /// <summary>Xoá mọi ảnh của một ngày (mặc định hôm nay).</summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete([FromQuery] DateOnly? date, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteProgressPhotosCommand(date), ct)));

    /// <summary>Nội dung ảnh cho URL do BE ký (dùng trong &lt;img src&gt;): chữ ký sai/hết hạn/ảnh không tồn tại đều 404.</summary>
    [HttpGet("{id:guid}/content")]
    [AllowAnonymous]
    // Trả nhị phân ảnh (không phải JSON) nên ngoại lệ duy nhất so với ActionResult<ApiResponse<T>>
    public async Task<IActionResult> GetContent(Guid id, [FromQuery] long exp, [FromQuery] string? sig, CancellationToken ct)
    {
        var content = await Sender.Send(new GetProgressPhotoContentQuery(id, exp, sig), ct);
        Response.Headers.CacheControl = "private, max-age=300";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(content.Data, content.ContentType);
    }
}
