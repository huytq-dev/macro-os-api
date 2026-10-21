namespace Application;

/// <summary>Cấu hình nút "+" của user; chưa lưu lần nào thì trả mặc định của BE (không ghi DB).</summary>
public sealed record GetShortcutsQuery : IRequest<ShortcutSettingsModel>;
