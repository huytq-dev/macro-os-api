namespace Application;

/// <summary>Xoá % mỡ Visual của một ngày (soft delete); không có → 404.</summary>
public sealed record DeleteVisualBodyFatCommand(DateOnly Date) : IRequest<Unit>;
