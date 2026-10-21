namespace Application;

/// <summary>UC-D16: xoá ghi chú (xoá thật; chỉ chủ ghi chú, người khác 404).</summary>
public sealed record DeleteFoodLogNoteCommand(Guid Id) : IRequest<Unit>;
