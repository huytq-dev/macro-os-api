namespace Application;

/// <summary>
/// Danh sách phiên ảnh theo ngày (mới nhất trước, chỉ ngày có ít nhất một ảnh), mỗi phiên kèm các tư thế (Front, Side, Back; URL xem có chữ ký như GET ?date=) và cân/trend của ngày đó.
/// From/To tuỳ chọn (gồm hai đầu), mặc định mọi ngày; Limit cắt theo SỐ PHIÊN (ngày mới nhất), tối đa 1000. Chỉ ảnh của chính user.
/// </summary>
public sealed record GetProgressPhotoSessionsQuery(DateOnly? From, DateOnly? To, int? Limit = null) : IRequest<ProgressPhotoSessionsResponse>;
