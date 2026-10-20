namespace Contract;

/// <param name="Angle">Front | Side | Back.</param>
/// <param name="Url">Đường dẫn tương đối <c>/api/progress-photos/{id}/content?exp=…&amp;sig=…</c> do BE ký, hết hạn tại <paramref name="ExpiresAt"/>; dùng thẳng trong &lt;img src&gt; (ghép với địa chỉ API). Không lưu lại, gọi lại GET để lấy URL mới.</param>
public sealed record ProgressPhotoModel(Guid Id, string Angle, string Url, DateTimeOffset ExpiresAt, DateTimeOffset CapturedAt);

/// <param name="HasConsent">Đã từng đồng ý lưu ảnh riêng tư; false thì lần tải đầu phải gửi consent = true.</param>
/// <param name="Photos">Thứ tự Front, Side, Back.</param>
/// <param name="ScaleWeightKg">Cân nặng của đúng ngày này nếu có.</param>
/// <param name="TrendWeightKg">Trend cân nặng của ngày này nếu tính được.</param>
public sealed record ProgressPhotosResponse(DateOnly Date, bool HasConsent, IReadOnlyList<ProgressPhotoModel> Photos, decimal? ScaleWeightKg = null, decimal? TrendWeightKg = null);

/// <summary>Một phiên ảnh (một ngày): các tư thế đã có (Front, Side, Back) kèm cân và trend của ngày đó.</summary>
public sealed record ProgressPhotoSessionModel(DateOnly Date, IReadOnlyList<ProgressPhotoModel> Photos, decimal? ScaleWeightKg, decimal? TrendWeightKg);

/// <param name="Sessions">Mới nhất trước; chỉ ngày có ít nhất một ảnh.</param>
public sealed record ProgressPhotoSessionsResponse(bool HasConsent, IReadOnlyList<ProgressPhotoSessionModel> Sessions);

/// <summary>Nội dung ảnh trả cho endpoint có chữ ký; không đi qua ApiResponse.</summary>
public sealed record ProgressPhotoContent(byte[] Data, string ContentType);
