namespace Contract;

/// <summary>
/// Cảnh báo KHÔNG chặn của target user tự nhập (Manual/Collaborative): 4·P + 4·C + 9·F lệch kcal quá dung sai. BE không bao giờ đổi P/F/C/kcal của user; FE hiện cảnh báo + kcal macro so với target + chênh lệch + giải thích và vẫn cho Lưu.
/// </summary>
/// <param name="Code">MACRO_KCAL_MISMATCH.</param>
/// <param name="Day">Thứ (Monday..Sunday) hoặc null.</param>
/// <param name="MacroKcal">4·P + 4·C + 9·F (làm tròn).</param>
/// <param name="DifferenceKcal">MacroKcal − Calories (dương = macro nhiều hơn kcal, âm = macro ít hơn).</param>
/// <param name="ToleranceKcal">Dung sai làm tròn đang dùng (TargetFeasibility.MacroEnergyToleranceKcal); chỉ cảnh báo khi |chênh lệch| lớn hơn.</param>
/// <param name="Message">Giải thích hành động được (tiếng Anh).</param>
public sealed record FeasibilityWarningModel(
    string Code,
    string? Day,
    int Calories,
    int MacroKcal,
    int DifferenceKcal,
    decimal ToleranceKcal,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    string Message);
