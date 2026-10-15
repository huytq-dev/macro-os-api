namespace Application;

internal static class MapsterExtensions
{
    // Quét mọi IRegister (*MappingConfig) trong Application vào GlobalSettings; handler dùng .Adapt<T>() trực tiếp, không inject IMapper.
    public static IServiceCollection AddMapsterServices(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(AssemblyReference.Assembly);
        return services;
    }
}
