namespace Application;

/// <summary>Về mặc định của BE: xoá dòng đã lưu (GET sau đó trả mặc định, IsDefault = true).</summary>
public sealed record ResetShortcutsCommand : IRequest<ShortcutSettingsModel>;
