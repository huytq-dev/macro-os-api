namespace Contract;

/// <summary>Ảnh tiến độ (dữ liệu nhạy cảm): giới hạn dung lượng và thời hạn URL xem ảnh (mục "ProgressPhotos" trong appsettings).</summary>
public sealed class ProgressPhotoOptions
{
    public const string SectionName = "ProgressPhotos";

    /// <summary>Dung lượng tối đa mỗi ảnh (byte), mặc định 10 MB.</summary>
    public int MaxBytes { get; init; } = 10 * 1024 * 1024;
    /// <summary>URL xem ảnh do BE ký hết hạn sau số phút này.</summary>
    public int SignedUrlMinutes { get; init; } = 15;
}
