using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure;

/// <summary>
/// Dự phòng của <see cref="IRedisCacheService"/> khi chưa cấu hình Upstash: lưu trong bộ nhớ của tiến trình.
/// Chỉ đúng với một instance; app khởi động lại thì token xác thực/đặt lại mật khẩu đang chờ sẽ mất
/// (người dùng bấm "Gửi lại email xác thực" là có token mới).
/// </summary>
public sealed class MemoryCacheService(IMemoryCache _cache) : IRedisCacheService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);

    public Task SetRecordAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var data = value is string text ? text : JsonSerializer.Serialize(value);
        _cache.Set(key, data, expiry ?? DefaultExpiry);
        return Task.CompletedTask;
    }

    public Task<T?> GetRecordAsync<T>(string key)
    {
        if (!_cache.TryGetValue(key, out string? data) || data is null) return Task.FromResult<T?>(default);

        var result = typeof(T) == typeof(string) ? (T)(object)data : JsonSerializer.Deserialize<T>(data);
        return Task.FromResult(result);
    }

    public Task RemoveRecordAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveManyAsync(params string[] keys)
    {
        foreach (var key in keys) _cache.Remove(key);
        return Task.CompletedTask;
    }
}
