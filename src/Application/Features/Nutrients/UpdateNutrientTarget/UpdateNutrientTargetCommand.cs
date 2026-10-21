namespace Application;

/// <summary>
/// UC-D09: ghi đè floor/target/ceiling của một nutrient (theo Code) NGUYÊN BỘ: null = không có mức đó (không thừa kế từ DRI/mặc định, xem <see cref="NutrientGoals"/>). Cả ba null = về mặc định.
/// Phải đúng floor ≤ target ≤ ceiling giữa các giá trị có mặt. Calories/P/F/C đặt qua program.
/// </summary>
public sealed record UpdateNutrientTargetCommand(
    // Nullable: route gán Code sau khi bind body; non-nullable thì model validation báo "Code field is required" trước controller.
    [property: System.Text.Json.Serialization.JsonIgnore] string? Code,
    decimal? Floor,
    decimal? Target,
    decimal? Ceiling
) : IRequest<Unit>;
