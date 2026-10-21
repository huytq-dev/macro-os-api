namespace Contract;

/// <summary>
/// Nguyên liệu: món + khẩu phần + số lượng (Quantity × gram của khẩu phần = lượng thực). FoodServingId null = Quantity là số GRAM (như log theo gram ở POST /food-logs/entries):
/// lượng thực giữ đúng số gram, khẩu phần lưu là khẩu phần mặc định của món. Dùng khi tạo recipe từ các entry đã log theo gram (Multi-Select → Create Recipe).
/// </summary>
public sealed record RecipeIngredientInput(Guid FoodItemId, Guid? FoodServingId, decimal Quantity, string? Notes);
