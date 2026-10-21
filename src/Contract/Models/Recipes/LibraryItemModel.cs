namespace Contract;

/// <summary>Dòng thư viện của tôi: món tự tạo hoặc recipe.</summary>
/// <param name="Kind">Food | Recipe.</param>
/// <param name="Calories">Theo khẩu phần mặc định (món) hoặc 1 khẩu phần (recipe) — cùng cho Protein/Carbs/Fat.</param>
/// <param name="DefaultServingId">Chỉ có với món; recipe log theo số khẩu phần, không có serving.</param>
public sealed record LibraryItemModel(
    string Kind,
    Guid Id,
    string Name,
    string? Brand,
    string? Emoji,
    Guid? DefaultServingId,
    string ServingName,
    decimal Calories,
    decimal Protein,
    decimal Carbs,
    decimal Fat,
    DateTimeOffset UpdatedAt,
    string? IconKey = null
);
