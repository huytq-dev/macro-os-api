using Mapster;
using MapsterMapper;

namespace Application;

internal static class MapsterExtensions
{
    public static IServiceCollection AddMapsterServices(this IServiceCollection services)
    {
        var assembly = AssemblyReference.Assembly;

        TypeAdapterConfig.GlobalSettings.Scan(assembly);

        services.AddSingleton(TypeAdapterConfig.GlobalSettings);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}