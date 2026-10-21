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
