namespace Contract;

/// <param name="Status">Confirmed sau khi xác nhận.</param>
/// <param name="Remembered">Số nguyên liệu đã ghi vào bộ nhớ đệm.</param>
public sealed record AiScanConfirmResponse(Guid ScanId, string Status, int Remembered);
