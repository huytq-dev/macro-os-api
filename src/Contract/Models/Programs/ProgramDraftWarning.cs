namespace Contract;

/// <param name="Code">MACRO_KCAL_MISMATCH (kcal từ macro lệch > 15% kcal đã nhập) | BELOW_FLOOR (kcal ngày thấp hơn sàn đã chọn).</param>
public sealed record ProgramDraftWarning(string Code, string? DayOfWeek);
