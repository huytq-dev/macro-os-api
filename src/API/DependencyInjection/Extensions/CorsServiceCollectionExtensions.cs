namespace API;

public static class CorsServiceCollectionExtensions
{
    public static IServiceCollection AddCORSPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var validOrigins = (configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToArray();

        if (validOrigins.Length == 0)
            throw new InvalidOperationException("AllowedOrigins must contain at least one valid origin.");

        services.AddCors(opts =>
            opts.AddPolicy("AllowFrontend", policy =>
                policy.WithOrigins(validOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders(DerivedDataStateHeader.Name)   // FE (khác origin) mới đọc được header trạng thái dẫn xuất
                      .AllowCredentials()));

        return services;
    }
}
