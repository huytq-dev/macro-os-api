using FluentValidation;

namespace API;

public static class FluentValidationExtensions
{
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);
        return services;
    }
}
