using InnovestHolding.Infrastructure.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InnovestHolding.Data;

public static class DbContextRegistration
{
    /// <summary>
    /// Registers AppDbContext WITH the home-cache invalidation interceptor. Program.cs and the test fixture both call
    /// this, so a fixture that replaces the context options cannot silently drop the interceptor.
    /// </summary>
    public static IServiceCollection AddAppDbContext(this IServiceCollection services, string? connectionString)
    {
        services.AddMemoryCache();
        services.TryAddSingleton<IHomeContentCache, HomeContentCache>();
        services.TryAddSingleton<ContentCacheInvalidationInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(sp.GetRequiredService<ContentCacheInvalidationInterceptor>()));
        return services;
    }
}
