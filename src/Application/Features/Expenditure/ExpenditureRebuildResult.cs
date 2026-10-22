namespace Application;

/// <param name="ResumeFrom">Ngày đầu tiên CHƯA dựng khi chạm ngân sách dòng (null = đã xong: hết khoảng hoặc đã hội tụ).</param>
/// <param name="RowsWritten">Số dòng snapshot (daily + expenditure) đã thêm/sửa.</param>
/// <param name="Converged">Dừng sớm vì snapshot tính lại đã trùng snapshot đang lưu.</param>
public sealed record ExpenditureRebuildResult(DateOnly? ResumeFrom, int RowsWritten, bool Converged);
