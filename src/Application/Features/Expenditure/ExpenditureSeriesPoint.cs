namespace Application;

/// <summary>Một ngày của chuỗi liên tục ở độ chính xác đầy đủ; Provisional = không có snapshot (giữ TDEE ngày trước).</summary>
public sealed record ExpenditureSeriesPoint(DateOnly Date, decimal Tdee, string Status, int WindowDays, bool Provisional);
