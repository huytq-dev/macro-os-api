namespace Contract;

/// <summary>Dòng kết quả tìm kiếm (phẳng).</summary>
/// <param name="Emoji">Emoji đại diện món suy từ nhóm món (cùng quy tắc với entry food-log); null cho nhóm Other.</param>
public sealed record FoodItemModel(
    Guid Id,
    string Name,
    string? Brand,
    string Category,
    string Source,
    bool IsVerified,
    bool IsMine,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    Guid? DefaultServingId,
    string DefaultServingName,
    decimal DefaultServingGrams,
    DateTimeOffset? LastLoggedAt,
    string? Emoji,
    string? IconKey = null
);
