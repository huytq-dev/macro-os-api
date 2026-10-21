namespace Contract;

/// <param name="Estimated">Luôn true: món/nguyên liệu/gram do AI nhận diện, FE hiện dòng cảnh báo.</param>
/// <param name="Confidence">low | medium | high, độ tin cậy chung do model tự đánh giá.</param>
/// <param name="ScanId">Id bản ghi phân tích đã lưu (kết quả, không lưu ảnh); gửi lại ở POST /food-logs/ai/confirm khi user log để hệ thống nhớ lựa chọn nguyên liệu.</param>
public sealed record AiAnalysisResponse(IReadOnlyList<AiDishModel> Dishes, bool Estimated, string Confidence, Guid? ScanId = null);
