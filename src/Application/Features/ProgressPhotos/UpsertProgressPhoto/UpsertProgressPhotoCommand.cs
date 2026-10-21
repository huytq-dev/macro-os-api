namespace Application;

/// <summary>
/// Lưu (hoặc thay) ảnh của (ngày, góc). Kiểm magic bytes (JPEG/PNG/WebP), bỏ EXIF/GPS, đẩy lên kho RIÊNG TƯ, chỉ lưu StorageKey. Ảnh không bao giờ được ghi log.
/// Lần đầu của user bắt buộc <paramref name="Consent"/> = true (ghi ConsentGivenAt).
/// </summary>
public sealed record UpsertProgressPhotoCommand(DateOnly? Date, PhotoAngle Angle, byte[] Data, bool Consent) : IRequest<ProgressPhotoModel>, ISensitiveRequest;
