namespace Application;

/// <summary>
/// Pha 1 của AI Plate: nhận diện món + nguyên liệu + gram từ ảnh (0–3) và/hoặc mô tả, KHÔNG có dinh dưỡng (pha 2 tìm từng nguyên liệu trong DB).
/// Lưu kết quả (không lưu ảnh) vào FoodScanAnalysis và trả ScanId. ISensitiveRequest để không log byte ảnh.
/// </summary>
public sealed record AnalyzeMealCommand(IReadOnlyList<MealPhoto> Photos, string? Description)
    : IRequest<AiAnalysisResponse>, ISensitiveRequest;
