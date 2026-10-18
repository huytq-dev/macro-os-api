namespace Contract;

/// <param name="Replayed">true = idempotency key đã dùng trước đó: không ghi gì mới, trả lại các entry của lần đầu.</param>
public sealed record BatchEntriesResponse(Guid BatchId, IReadOnlyList<Guid> EntryIds, bool Replayed);
