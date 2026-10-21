namespace Contract;

/// <param name="Angle">Front | Side | Back.</param>
/// <param name="Url">Đường dẫn tương đối <c>/api/progress-photos/{id}/content?exp=…&amp;sig=…</c> do BE ký, hết hạn tại <paramref name="ExpiresAt"/>; dùng thẳng trong &lt;img src&gt; (ghép với địa chỉ API). Không lưu lại, gọi lại GET để lấy URL mới.</param>
public sealed record ProgressPhotoModel(Guid Id, string Angle, string Url, DateTimeOffset ExpiresAt, DateTimeOffset CapturedAt);
