using System.Text.Json;

namespace Application;

/// <summary>"Create &amp; Add": tạo recipe rồi log nó (một lời gọi). Hai bước là hai transaction: nếu log lỗi thì recipe vẫn được tạo (lỗi trả về, FE gọi lại POST /food-logs/entries với recipeId).</summary>
public sealed record CreateAndLogRecipeCommand(CreateRecipeCommand Recipe, RecipeLogOptions? Log = null) : IRequest<RecipeCreatedResponse>;
