namespace Contract;

/// <param name="Key">intro | partial-logging | program-update; thứ tự mảng = thứ tự hiển thị.</param>
/// <param name="State">required | skippable | hidden.</param>
public sealed record CheckInModuleModel(string Key, string State);
