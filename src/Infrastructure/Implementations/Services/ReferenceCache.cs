using System.Collections.Concurrent;

namespace Infrastructure;

/// <summary>Cache tham chiếu trong bộ nhớ tiến trình (singleton): xem <see cref="IReferenceCache"/>. Thread-safe; mục hết hạn bị bỏ khi đọc.</summary>
[RegisterService(typeof(IReferenceCache), ServiceLifetime.Singleton)]
public sealed class ReferenceCache(TimeProvider time) : IReferenceCache
{
    private const int MaxEntries = 4000;
    private readonly ConcurrentDictionary<string, (DateTimeOffset Expires, object Value)> _items = new();

    public bool TryGet<T>(string key, out T value)
    {
        if (_items.TryGetValue(key, out var item) && item.Expires > time.GetUtcNow() && item.Value is T typed)
        {
            value = typed;
            return true;
        }
        value = default!;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan ttl)
    {
        if (_items.Count >= MaxEntries) _items.Clear(); // ponytail: xoá hết khi đầy, đủ cho vài nghìn món hay log; LRU khi thật sự cần
        _items[key] = (time.GetUtcNow() + ttl, value!);
    }
}

/// <summary>Không cache: UnitOfWork dựng thủ công (test, công cụ dòng lệnh) luôn đọc DB, tránh lẫn dữ liệu giữa các DB trong cùng process.</summary>
internal sealed class NoReferenceCache : IReferenceCache
{
    public static readonly NoReferenceCache Instance = new();
    public bool TryGet<T>(string key, out T value) { value = default!; return false; }
    public void Set<T>(string key, T value, TimeSpan ttl) { }
}
