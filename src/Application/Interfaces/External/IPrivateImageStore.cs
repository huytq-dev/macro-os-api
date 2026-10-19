namespace Application;

/// <summary>
/// Kho ảnh RIÊNG TƯ (không có URL công khai): ảnh chỉ đọc được qua BE sau khi kiểm quyền. Khác <see cref="ICloudinaryService"/> (ảnh đại diện, công khai).
/// Lỗi mạng/dịch vụ → <see cref="ExternalServiceException"/>.
/// </summary>
public interface IPrivateImageStore
{
    /// <summary>Ghi (đè nếu đã có) ảnh dưới khoá <paramref name="storageKey"/>.</summary>
    Task UploadAsync(string storageKey, byte[] data, CancellationToken ct);

    /// <returns>Nội dung ảnh, hoặc null nếu không tồn tại.</returns>
    Task<byte[]?> DownloadAsync(string storageKey, CancellationToken ct);

    /// <summary>Xoá ảnh; không tồn tại thì coi như đã xoá.</summary>
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
