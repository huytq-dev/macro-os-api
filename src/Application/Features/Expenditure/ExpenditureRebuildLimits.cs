namespace Application;

/// <summary>
/// Giới hạn cho lần dựng đồng bộ ngắn của <see cref="ExpenditureRebuilder"/> (không truyền = dựng hết khoảng).
/// </summary>
/// <param name="ConvergeAfter">Từ ngày này (ngày đầu tiên mà cửa sổ tính không còn chứa dữ liệu vừa sửa), nếu snapshot tính lại giống snapshot đang lưu suốt một cửa sổ liên tiếp thì dừng:
/// phần đuôi (EMA) chỉ co dần nên không còn gì đổi. Tầm dừng suy ra từ dữ liệu, không phải số ngày cố định.</param>
/// <param name="RowBudget">Trần số dòng snapshot ghi; chạm trần thì dừng và trả <see cref="ExpenditureRebuildResult.ResumeFrom"/> để phần còn lại thành job nền.</param>
public sealed record ExpenditureRebuildLimits(DateOnly? ConvergeAfter = null, int RowBudget = int.MaxValue);
