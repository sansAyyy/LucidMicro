using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LucidMicro.BuildingBlocks.Outbox.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidOutboxPublisher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(new OutboxPublisherOptions());
        services.TryAddScoped<IOutboxPublisher, DefaultOutboxPublisher>();

        return services;
    }

    public static IServiceCollection AddLucidOutboxPublisherHostedService(
        this IServiceCollection services,
        Action<OutboxPublisherOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new OutboxPublisherOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddLucidOutboxPublisher();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, OutboxPublisherHostedService>());

        return services;
    }
}
