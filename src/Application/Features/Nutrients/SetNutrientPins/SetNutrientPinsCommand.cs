namespace Application;

/// <summary>UC-D09: đặt danh sách nutrient pin lên Dashboard theo thứ tự (tối đa 8); thay toàn bộ pin hiện có. Danh sách rỗng = bỏ hết pin.</summary>
public sealed record SetNutrientPinsCommand(IReadOnlyList<string> Codes) : IRequest<Unit>;
