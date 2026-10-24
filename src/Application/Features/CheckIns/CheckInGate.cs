namespace Application;

/// <summary>
/// Cổng của Accept check-in (G6, user duyệt): đề xuất target tuần mới dựa trên TDEE trong snapshot, nên KHÔNG được chấp nhận khi dữ liệu dẫn xuất chưa tính xong
/// (<see cref="DerivedDataStatusResponse"/> khác Ready). Reject không dùng TDEE (chỉ chép target đã chấp nhận) nên luôn được phép. Hàm thuần.
/// </summary>
public static class CheckInGate
{
    public const string Updating = "DERIVED_DATA_UPDATING", Failed = "DERIVED_DATA_FAILED";

    /// <summary>null = Accept được phép; ngược lại mã lý do (Updating/Failed). Trạng thái lạ coi như chưa sẵn sàng (đóng cổng, không mở nhầm).</summary>
    public static string? AcceptBlockReason(string derivedDataState) => derivedDataState switch
    {
        DerivedDataStatusResponse.Ready => null,
        DerivedDataStatusResponse.Failed => Failed,
        _ => Updating,
    };

    public static IDictionary<string, string[]> Errors(string reasonCode) => new Dictionary<string, string[]> { ["reasonCode"] = [reasonCode] };
}
