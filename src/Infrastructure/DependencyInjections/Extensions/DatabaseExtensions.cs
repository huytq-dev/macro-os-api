namespace Infrastructure;

internal static class DatabaseExtensions
{
    public static IServiceCollection AddInfrastructureDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not set in appsettings.json");

        services.AddDbContextPool<ApplicationDbContext>((sp, options) =>
        {
            // Include nhiều collection (Program → Weeks + DayTargets…) tách thành truy vấn riêng, không nhân tích Descartes (.rules: tránh Include nhiều collection cùng truy vấn)
            options.UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            options.ConfigureWarnings(w =>
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });
        return services;
    }
}
