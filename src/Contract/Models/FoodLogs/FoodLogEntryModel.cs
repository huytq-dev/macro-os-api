namespace Contract;

/// <param name="Quantity">Số serving nếu có FoodServingId, ngược lại là số gram.</param>
/// <param name="Emoji">Emoji đại diện món suy từ nhóm món; null cho nhóm Other, recipe, quick add (FE dùng 🍽️).</param>
/// <param name="WeightGrams">Tổng khối lượng dòng log (gram): số serving × gram của serving, log theo gram thì = quantity; null cho recipe và quick add.</param>
/// <param name="EatenAt">Giờ ăn; luôn có giá trị trong GET /food-logs (dòng cũ không có giờ lấy giờ mặc định của bữa).</param>
/// <param name="EntrySource">Food (món thường) | Recipe | QuickAdd: FE dùng để hiển thị đúng (không suy từ weightGrams/servingName null).</param>
/// <param name="QuantityUnit">Đơn vị của <c>Quantity</c>: Gram (log theo gram) | Serving (số serving của món, hoặc số phần của recipe) | Entry (quick add: một dòng nhập tay, không có khối lượng).</param>
public sealed record FoodLogEntryModel(
    Guid Id,
    string MealType,
    TimeOnly? EatenAt,
    Guid? FoodItemId,
    Guid? RecipeId,
    string FoodName,
    Guid? FoodServingId,
    string? ServingName,
    decimal Quantity,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    decimal? Fiber,
    string? Emoji,
    decimal? WeightGrams,
    string? IconKey = null,
    string EntrySource = "Food",
    string QuantityUnit = "Gram"
);
