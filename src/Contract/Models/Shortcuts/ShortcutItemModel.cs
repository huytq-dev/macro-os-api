namespace Contract;

/// <param name="Key">Khoá cố định của BE (search, barcode, ai, weight, recipes, quick-add, photos, metrics, your-foods, new-recipe, new-food, favorites); nhãn/icon do FE giữ.</param>
/// <param name="Color">red | orange | yellow | green | teal | blue | purple, hoặc null = không màu.</param>
public sealed record ShortcutItemModel(string Key, bool Enabled, string? Color);
