namespace Contract;

/// <summary>
/// Số "X" của câu hỏi Prior Weight ("Have you ever weighed more than X?"), tính MỘT lần ở BE để FE và BE dùng chung.
/// <paramref name="Policy"/> = MACROOS_PROVISIONAL_POLICY: công thức tạm của MacroOS (cân / 0.9), KHÔNG phải công thức hiển thị đã xác minh của MacroFactor; nếu MF khác sẽ báo để duyệt, không tự đổi.
/// </summary>
/// <param name="WeightKg">Cân dùng để dựng câu hỏi (cân gửi kèm hoặc cân gần nhất).</param>
/// <param name="ThresholdKgExact">Ngưỡng chính xác (kg), làm tròn lên 4 chữ số; giá trị dùng để xét điều kiện.</param>
/// <param name="DisplayValue">Số để HIỂN THỊ (làm tròn LÊN số nguyên theo đơn vị) — FE hiển thị đúng số này và gửi lại khi lưu đáp án.</param>
/// <param name="DisplayUnit">kg | lb.</param>
public sealed record PriorWeightQuestionResponse(decimal WeightKg, decimal ThresholdKgExact, decimal DisplayValue, string DisplayUnit, string Policy);
