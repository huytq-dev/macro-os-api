namespace Application;

/// <summary>Nhận diện món/nguyên liệu từ ảnh và/hoặc mô tả; ước lượng dinh dưỡng một nguyên liệu khi DB không có.</summary>
public interface IAiMealAnalyzer
{
    /// <param name="canonicalNames">Tên chuẩn tiếng Anh trong catalog để model ưu tiên dùng (chỉ tên, không macro); rỗng thì model tự đặt tên chung.</param>
    /// <returns>Bản nháp từ model, chỉ cấu trúc món + nguyên liệu + gram; lỗi mạng/timeout/trả sai định dạng → <see cref="ExternalServiceException"/>.</returns>
    Task<AiMealDraft> AnalyzeAsync(IReadOnlyList<MealPhoto> photos, string? description, IReadOnlyList<string> canonicalNames, CancellationToken ct);

    /// <returns>Ước lượng cho <paramref name="grams"/> gram <paramref name="name"/>; lỗi → <see cref="ExternalServiceException"/>.</returns>
    Task<AiIngredientEstimate> EstimateIngredientAsync(string name, decimal grams, CancellationToken ct);
}
