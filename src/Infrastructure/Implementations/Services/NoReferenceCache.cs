using System.Collections.Concurrent;

namespace Infrastructure;

/// <summary>Không cache: UnitOfWork dựng thủ công (test, công cụ dòng lệnh) luôn đọc DB, tránh lẫn dữ liệu giữa các DB trong cùng process.</summary>
internal sealed class NoReferenceCache : IReferenceCache
{
    public static readonly NoReferenceCache Instance = new();
    public bool TryGet<T>(string key, out T value) { value = default!; return false; }
    public void Set<T>(string key, T value, TimeSpan ttl) { }
}
