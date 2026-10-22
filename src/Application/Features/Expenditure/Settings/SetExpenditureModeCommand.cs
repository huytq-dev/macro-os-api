namespace Application;

/// <summary>
/// Chuyển Dynamic ↔ Static, hiệu lực từ HÔM NAY (ngày local của user). Static cần <c>StaticTdeeKcal</c> (1000–6000); Dynamic không mang giá trị.
/// Overlay: engine tiếp tục tính expenditure học được, không reset gì; chuyển lại trong cùng ngày ghi đè bản của ngày đó.
/// </summary>
public sealed record SetExpenditureModeCommand(ExpenditureMode Mode, int? StaticTdeeKcal) : IRequest<ExpenditureSettingsResponse>;
