namespace Application;

/// <summary>UC-D16: thêm ghi chú (nội dung + tên + màu) cho một ngày, tối đa 5/ngày (409 khi đủ). LogDate null = hôm nay theo múi giờ user; NotedAt thiếu → giờ hiện tại của user (cùng quy tắc giờ của entry); Color thiếu → Gray.</summary>
public sealed record AddFoodLogNoteCommand(DateOnly? LogDate, string Text, TimeOnly? NotedAt, string? Name = null, NoteColor? Color = null) : IRequest<Guid>;
