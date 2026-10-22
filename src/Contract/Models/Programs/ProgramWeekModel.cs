namespace Contract;

public sealed record ProgramWeekModel(
    Guid Id,
    DateOnly EffectiveFrom,
    int CaloriesTarget,
    decimal ProteinTarget,
    decimal CarbsTarget,
    decimal FatTarget,
    string AdjustmentReason,
    int? PreviousCaloriesTarget,
    string? TargetProvenance = null
);
// TargetProvenance (D2, tên thống nhất cho MỌI API trả target theo ngày): VERIFIED = lấy từ snapshot 7 ngày của đúng phiên bản tuần hiệu lực (lịch sử đã kiểm chứng);
// LEGACY_UNVERIFIED = tuần cũ trước D2, target ngày dựng từ cấu hình HIỆN TẠI nên KHÔNG kiểm chứng được (chỉ để tham khảo: nhãn "Chưa xác minh", không badge đạt/không đạt, không vào thống kê/streak);
// null = không có target hoặc không áp dụng. (Trong danh sách tuần của ProgramResponse: VERIFIED nếu tuần có snapshot.)
