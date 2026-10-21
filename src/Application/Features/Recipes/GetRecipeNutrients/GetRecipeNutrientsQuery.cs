using System.Text.Json;

namespace Application;

/// <summary>
/// Create Recipe step 1: dinh dưỡng của recipe đang soạn (chưa lưu). Nguyên liệu như lúc tạo: FoodServingId null = Quantity là gram.
/// Trả tổng và theo khẩu phần (tổng ÷ ServingsCount), mỗi bên đủ danh mục chất kèm % target của ngày <c>Date</c> (mặc định hôm nay theo múi giờ user). Chỉ đọc.
/// </summary>
public sealed record GetRecipeNutrientsQuery(List<RecipeIngredientInput> Ingredients, int ServingsCount = 1, DateOnly? Date = null) : IRequest<RecipeNutrientsResponse>;
