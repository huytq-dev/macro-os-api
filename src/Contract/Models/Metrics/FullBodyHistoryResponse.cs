namespace Contract;

/// <param name="Items">Chỉ các ngày có dữ liệu, mới nhất trước.</param>
public sealed record FullBodyHistoryResponse(IReadOnlyList<FullBodyHistoryItem> Items);
