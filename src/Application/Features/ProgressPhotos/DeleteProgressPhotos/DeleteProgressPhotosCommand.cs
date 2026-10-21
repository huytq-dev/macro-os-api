namespace Application;

/// <summary>
/// Xoá ảnh của một ngày (mặc định hôm nay theo múi giờ user): không có Angle = cả phiên (mọi tư thế), có Angle = chỉ tư thế đó. Xoá mềm dòng DB và xoá file khỏi kho riêng tư; không có gì để xoá vẫn thành công.
/// </summary>
public sealed record DeleteProgressPhotosCommand(DateOnly? Date, PhotoAngle? Angle = null) : IRequest<Unit>;
