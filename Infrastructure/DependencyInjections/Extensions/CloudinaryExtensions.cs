using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

internal static class CloudinaryExtensions
{
    public static IServiceCollection AddCloudinaryService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CloudinaryOptions>(configuration.GetSection("Cloudinary"));
        //services.AddScoped<IPhotoService, CloudinaryService>();
        return services;
    }
}
