namespace Application;

/// <param name="Ranges">Danh sách các kỳ SAU khi sửa (giữ nguyên Id của kỳ còn lại; kỳ mới do tách có Id mới).</param>
/// <param name="Changed">false = không có gì đổi (bật ngày đã được theo dõi, tắt ngày chưa theo dõi).</param>
public sealed record PeriodEdit(IReadOnlyList<PeriodRange> Ranges, bool Changed);
