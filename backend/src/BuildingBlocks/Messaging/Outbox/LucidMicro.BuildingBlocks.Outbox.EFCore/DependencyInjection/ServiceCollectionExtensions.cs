using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Serialization;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Services;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Options;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LucidMicro.BuildingBlocks.Outbox.EFCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidEfCoreOutbox<TDbContext>(
        this IServiceCollection services,
        Action<EfCoreOutboxOptions>? configureOptions = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EfCoreOutboxOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(options);
        services.TryAddScoped<IOutboxEventWriter, DefaultOutboxEventWriter>();
        services.TryAddScoped<IOutboxMessageSerializer, SystemTextJsonOutboxMessageSerializer>();
        services.AddScoped<IOutboxMessageStore, EfCoreOutboxMessageStore<TDbContext>>();

        return services;
    }
}
