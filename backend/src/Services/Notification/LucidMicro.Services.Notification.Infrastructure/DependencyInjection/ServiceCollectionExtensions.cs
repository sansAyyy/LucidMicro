using LucidMicro.BuildingBlocks.Auth.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Options;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.DependencyInjection;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Options;
using LucidMicro.BuildingBlocks.HealthChecks.Consul.DependencyInjection;
using LucidMicro.BuildingBlocks.HealthChecks.Npgsql.DependencyInjection;
using LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ.DependencyInjection;
using LucidMicro.BuildingBlocks.Inbox.EFCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Persistence.EFCore.DependencyInjection;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.DependencyInjection;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using LucidMicro.Contracts.Notification.IntegrationEvents;
using LucidMicro.Services.Notification.Application.Abstractions;
using LucidMicro.Services.Notification.Application.Features.Notifications.IntegrationEvents;
using LucidMicro.Services.Notification.Infrastructure.Persistence;
using LucidMicro.Services.Notification.Infrastructure.Sending;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LucidMicro.Services.Notification.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddPersistence(services, configuration);
        AddAuth(services, configuration);
        AddInbox(services);
        AddServiceDiscovery(services, configuration);
        AddMessaging(services, configuration);
        AddHealthChecks(services);
        AddSending(services);

        return services;
    }

    private static void AddAuth(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLucidJwtAuthentication(
            configuration.GetRequiredSection(JwtAccessTokenOptions.ConfigurationSectionName));
    }

    private static void AddPersistence(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Notification")
            ?? throw new InvalidOperationException("Connection string 'Notification' is not configured.");

        services.AddLucidEfCorePersistence<NotificationDbContext>(
            options => options.UseNpgsql(connectionString));
    }

    private static void AddInbox(IServiceCollection services)
    {
        services.AddLucidEfCoreInbox<NotificationDbContext>();
    }

    private static void AddServiceDiscovery(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLucidConsulServiceDiscovery(
            configuration.GetRequiredSection(LucidConsulServiceDiscoveryOptions.ConfigurationSectionName));
        services.AddLucidConsulServiceRegistration(
            configuration.GetRequiredSection(LucidConsulServiceRegistrationOptions.ConfigurationSectionName));
    }

    private static void AddMessaging(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLucidRabbitMqEventBus(
            configuration.GetRequiredSection(RabbitMqEventBusOptions.ConfigurationSectionName));
        services.AddLucidRabbitMqConsumer<
            NotificationSendRequestedIntegrationEvent,
            NotificationSendRequestedIntegrationEventHandler>();
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        services.AddLucidNpgsqlDbContextHealthCheck<NotificationDbContext>();
        services.AddLucidRabbitMqHealthCheck();
        services.AddLucidConsulHealthCheck();
    }

    private static void AddSending(IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<INotificationSender, DefaultNotificationSender>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<INotificationChannelSender, LogNotificationChannelSender>());
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<INotificationChannelSender, LogSmsNotificationChannelSender>());
    }
}
