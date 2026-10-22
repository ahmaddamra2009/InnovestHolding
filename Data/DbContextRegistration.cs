using InnovestHolding.Infrastructure;
using InnovestHolding.Infrastructure.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InnovestHolding.Data;

public static class DbContextRegistration
{
    /// <summary>
    /// Registers AppDbContext WITH the site-cache invalidation interceptor. Program.cs and the test fixture both call
    /// this, so a fixture that replaces the context options cannot silently drop the interceptor.
    /// </summary>
    public static IServiceCollection AddAppDbContext(this IServiceCollection services, string? connectionString)
    {
        services.AddMemoryCache();
        services.TryAddSingleton<ISiteContentCache, SiteContentCache>();
        services.TryAddSingleton<ContentCacheInvalidationInterceptor>();
        services.TryAddSingleton<ProductionDbGuardInterceptor>();
        // The production-database guard runs on every connection open (the app, the tests and the EF tools all build
        // the context through here), so `dotnet ef --connection` cannot bypass it.
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(
                sp.GetRequiredService<ProductionDbGuardInterceptor>(),
                sp.GetRequiredService<ContentCacheInvalidationInterceptor>()));
        return services;
    }
}
