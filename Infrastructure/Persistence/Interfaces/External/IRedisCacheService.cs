namespace Infrastructure;

public interface IRedisCacheService
{
    Task SetRecordAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetRecordAsync<T>(string key);
    Task RemoveRecordAsync(string key);
    Task RemoveManyAsync(params string[] keys);
}
