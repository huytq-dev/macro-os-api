namespace Contract;

/// <param name="NotedAt">Giờ gắn với ghi chú; luôn có giá trị với ghi chú tạo qua API.</param>
/// <param name="Name">"Note Name"; null nếu không đặt.</param>
/// <param name="Color">Gray | Red | Blue | Green | Yellow | Purple | Orange (chuỗi); mặc định Gray.</param>
public sealed record FoodLogNoteModel(Guid Id, string Text, TimeOnly? NotedAt, DateTimeOffset CreatedAt, string? Name, string Color);
