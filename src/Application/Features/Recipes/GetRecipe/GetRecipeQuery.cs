using System.Text.Json;

namespace Application;

/// <summary>WP4: chi tiết recipe của mình: nguyên liệu kèm tên món/khẩu phần, tổng và theo khẩu phần.</summary>
public sealed record GetRecipeQuery(Guid Id) : IRequest<RecipeResponse>;
