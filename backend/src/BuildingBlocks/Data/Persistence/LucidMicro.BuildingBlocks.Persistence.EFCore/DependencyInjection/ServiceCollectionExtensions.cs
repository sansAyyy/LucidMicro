using LucidMicro.BuildingBlocks.Persistence.Abstractions.Auditing;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Conflicts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Auditing;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Conflicts;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Interceptors;
using LucidMicro.BuildingBlocks.Persistence.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidEfCorePersistence<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
        where TDbContext : DbContext, IUnitOfWork
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services.AddLucidEfCorePersistence<TDbContext>((_, options) => configureOptions(options));
    }

    public static IServiceCollection AddLucidEfCorePersistence<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureOptions)
        where TDbContext : DbContext, IUnitOfWork
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IAuditUserProvider, DefaultAuditUserProvider>();
        services.TryAddScoped<IPersistenceConflictDetector, EfCorePersistenceConflictDetector>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<TDbContext>((serviceProvider, options) =>
        {
            configureOptions(serviceProvider, options);
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<DbContext>(serviceProvider => serviceProvider.GetRequiredService<TDbContext>());
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork<TDbContext>>();
        services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(EfReadOnlyRepository<,>));
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        return services;
    }
}
