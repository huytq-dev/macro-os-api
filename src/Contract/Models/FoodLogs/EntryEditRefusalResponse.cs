namespace Contract;

/// <summary>
/// Body <c>data</c> của lỗi 400 khi BE từ chối sửa entry vì thiếu dữ liệu gốc để giữ nguyên lịch sử dinh dưỡng (envelope chuẩn: isSuccess=false, statusCode=400, message, errors, data).
/// <c>ReasonCode</c> hiện chỉ có <see cref="LegacyServingSnapshotMissing"/>: entry cũ (log trước khi có snapshot khẩu phần) không thể đổi khẩu phần vì không xác minh được khối lượng gốc;
/// đổi số lượng, bữa, giờ vẫn được phép. Dinh dưỡng của entry KHÔNG bị thay đổi khi bị từ chối. Cách xử lý ở FE: xoá và log lại món, hoặc chỉ đổi số lượng.
/// </summary>
public sealed record EntryEditRefusalResponse(string ReasonCode, Guid EntryId)
{
    public const string LegacyServingSnapshotMissing = "LEGACY_SERVING_SNAPSHOT_MISSING";
}
