namespace Application;

/// <summary>Xoá sheet Metrics của một ngày (mặc định hôm nay): cả hàng Visual lẫn Tape (soft delete).</summary>
public sealed record DeleteFullBodyCommand(DateOnly? Date) : IRequest<Unit>;
