namespace Application;

/// <summary>Ảnh bữa ăn đã đọc vào bộ nhớ; chỉ sống trong request, không lưu đâu cả.</summary>
public sealed record MealPhoto(byte[] Data)
{
    /// <summary>image/jpeg | image/png | image/webp theo byte đầu file (không tin Content-Type client gửi); null nếu không nhận ra.</summary>
    public string? MediaType => ImageSniffer.Detect(Data);
}

/// <summary>Nguyên liệu thô do model trả, chưa làm sạch.</summary>
public sealed record AiDraftIngredient(string Name, decimal Grams);

public sealed record AiDraftDish(string Name, IReadOnlyList<AiDraftIngredient> Ingredients);

public sealed record AiMealDraft(IReadOnlyList<AiDraftDish> Dishes, string? Confidence);

/// <summary>Dinh dưỡng ước lượng cho TOÀN BỘ khối lượng đã hỏi (không phải /100 g).</summary>
public sealed record AiIngredientEstimate(decimal Calories, decimal Protein, decimal Fat, decimal Carbs);

/// <summary>Nhận diện món/nguyên liệu từ ảnh và/hoặc mô tả; ước lượng dinh dưỡng một nguyên liệu khi DB không có.</summary>
public interface IAiMealAnalyzer
{
    /// <returns>Bản nháp từ model, chỉ cấu trúc món + nguyên liệu + gram; lỗi mạng/timeout/trả sai định dạng → <see cref="ExternalServiceException"/>.</returns>
    Task<AiMealDraft> AnalyzeAsync(IReadOnlyList<MealPhoto> photos, string? description, CancellationToken ct);

    /// <returns>Ước lượng cho <paramref name="grams"/> gram <paramref name="name"/>; lỗi → <see cref="ExternalServiceException"/>.</returns>
    Task<AiIngredientEstimate> EstimateIngredientAsync(string name, decimal grams, CancellationToken ct);
}
