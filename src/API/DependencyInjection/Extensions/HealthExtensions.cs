using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API;

/// <summary>Kiểm tra sức khoẻ cho giám sát/uptime: app đang chạy và kết nối được DB. Công khai: GET /api/health.</summary>
public static class HealthExtensions
{
    public const string Path = "/api/health";

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
        return services;
    }

    public static WebApplication MapApiHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(Path, new HealthCheckOptions { ResponseWriter = WriteResponseAsync }).AllowAnonymous();
        return app;
    }

    // Cùng khung ApiResponse với các API khác; chỉ trả trạng thái, không lộ chi tiết lỗi ra ngoài.
    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        var healthy = report.Status == HealthStatus.Healthy;
        var body = new
        {
            isSuccess = healthy,
            statusCode = healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
            message = report.Status.ToString(),
            data = new
            {
                status = report.Status.ToString(),
                totalDurationMs = (long)report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new { status = entry.Value.Status.ToString(), durationMs = (long)entry.Value.Duration.TotalMilliseconds })
            }
        };

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonSerializerOptions.Web));
    }
}

internal sealed class DatabaseHealthCheck(ApplicationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        => await db.Database.CanConnectAsync(ct)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to the database.");
}
