using LucidMicro.BuildingBlocks.EventBus.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Consumers;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Options;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LucidMicro.BuildingBlocks.EventBus.RabbitMQ.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidRabbitMqEventBus(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = RabbitMqEventBusOptions.FromConfiguration(configurationSection);
        options.Validate();

        services
            .AddOptions<RabbitMqEventBusOptions>()
            .Bind(configurationSection)
            .Validate(ValidateOptions, "Lucid RabbitMQ event bus options are invalid.")
            .ValidateOnStart();

        services.AddSingleton(options);
        services.AddSingleton<RabbitMqEventBus>();
        services.AddSingleton<IEventBus>(serviceProvider => serviceProvider.GetRequiredService<RabbitMqEventBus>());
        services.AddSingleton<IIntegrationEventEnvelopePublisher>(
            serviceProvider => serviceProvider.GetRequiredService<RabbitMqEventBus>());

        return services;
    }

    private static bool ValidateOptions(RabbitMqEventBusOptions options)
    {
        try
        {
            options.Validate();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static IServiceCollection AddLucidRabbitMqConsumer<TEvent, THandler>(
        this IServiceCollection services,
        string? queueName = null,
        bool requeueOnFailure = false)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        ThrowIfConsumerAlreadyRegistered<TEvent, THandler>(services, queueName);

        services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
        services.AddSingleton(
            RabbitMqConsumerRegistration.Create<TEvent, THandler>(
                queueName,
                requeueOnFailure));
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, RabbitMqConsumerHostedService>());

        return services;
    }

    private static void ThrowIfConsumerAlreadyRegistered<TEvent, THandler>(
        IServiceCollection services,
        string? queueName)
        where TEvent : IntegrationEvent
    {
        var normalizedQueueName = string.IsNullOrWhiteSpace(queueName) ? null : queueName;
        var alreadyRegistered = services
            .Where(service => service.ServiceType == typeof(RabbitMqConsumerRegistration))
            .Select(service => service.ImplementationInstance)
            .OfType<RabbitMqConsumerRegistration>()
            .Any(registration => registration.EventType == typeof(TEvent)
                                 && registration.HandlerType == typeof(THandler)
                                 && registration.QueueName == normalizedQueueName);

        if (alreadyRegistered)
        {
            throw new InvalidOperationException(
                $"RabbitMQ consumer has already been registered: event '{typeof(TEvent).Name}', handler '{typeof(THandler).Name}'.");
        }
    }
}
