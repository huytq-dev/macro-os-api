using System.Net.Http.Headers;

namespace Infrastructure;

internal static class RedisExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Upstash:BaseUrl"];
        var token = configuration["Upstash:Token"];

        // Chưa cấu hình Upstash (dev, hoặc database Upstash đã bị xoá): dùng bộ nhớ tiến trình thay vì Redis
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
        {
            services.AddMemoryCache();
            services.AddSingleton<IRedisCacheService, MemoryCacheService>();
            return services;
        }

        services.AddHttpClient<IRedisCacheService, RedisCacheService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                    MaxConnectionsPerServer = 100,
                    UseProxy = false,
                    Proxy = null
                });
        return services;
    }
}
