namespace Application;

/// <summary>UC-D16: sửa ghi chú (chỉ chủ ghi chú, người khác 404). Trường không gửi (null) thì giữ cũ: NotedAt, Name, Color; Name = "" để xoá tên.</summary>
public sealed record UpdateFoodLogNoteCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    string Text,
    TimeOnly? NotedAt,
    string? Name = null,
    NoteColor? Color = null
) : IRequest<Unit>;
