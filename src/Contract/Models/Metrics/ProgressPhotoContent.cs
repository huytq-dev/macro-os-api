namespace Contract;

/// <summary>Nội dung ảnh trả cho endpoint có chữ ký; không đi qua ApiResponse.</summary>
public sealed record ProgressPhotoContent(byte[] Data, string ContentType);
