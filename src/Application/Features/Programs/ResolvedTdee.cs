namespace Application;

/// <summary>
/// TDEE khởi điểm cho Setup → Preview → Review → Create, cùng một nguồn (<see cref="InitialTdeeResolver"/>).
/// </summary>
/// <param name="Source">Một trong <see cref="TdeeSources"/>.</param>
/// <param name="IsStale">Đang có job dựng lại dữ liệu dẫn xuất (status Updating/Failed): số expenditure hiện tại có thể chưa khớp dữ liệu nguồn mới nhất.</param>
/// <param name="Formula">Ước tính theo hồ sơ (Cunningham/Mifflin): luôn có, dùng cho sàn kcal (BMR) và fallback.</param>
public sealed record ResolvedTdee(int TdeeKcal, string Source, bool IsStale, TdeeEstimate? Formula);
