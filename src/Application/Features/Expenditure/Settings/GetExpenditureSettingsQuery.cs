namespace Application;

/// <summary>Cài đặt Expenditure hiện hiệu lực (chỉ đọc, KHÔNG tạo dòng settings).</summary>
public sealed record GetExpenditureSettingsQuery : IRequest<ExpenditureSettingsResponse>;
