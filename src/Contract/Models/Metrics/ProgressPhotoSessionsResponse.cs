namespace Contract;

/// <param name="Sessions">Mới nhất trước; chỉ ngày có ít nhất một ảnh.</param>
public sealed record ProgressPhotoSessionsResponse(bool HasConsent, IReadOnlyList<ProgressPhotoSessionModel> Sessions);
