namespace Contract;

/// <param name="Available">false = Open Food Facts lỗi/timeout/bị tắt trong cài đặt user: nhóm rỗng, FE ẩn nhóm (không coi là "không có kết quả").</param>
public sealed record OffSearchGroup(IReadOnlyList<OffSearchItem> Items, int TotalCount, bool Available)
{
    public static readonly OffSearchGroup Unavailable = new([], 0, false);
}
