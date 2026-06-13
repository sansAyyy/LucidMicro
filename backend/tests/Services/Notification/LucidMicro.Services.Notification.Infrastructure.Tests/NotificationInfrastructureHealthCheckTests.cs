using LucidMicro.BuildingBlocks.Inbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;
using LucidMicro.BuildingBlocks.Inbox.EFCore.Stores;
using LucidMicro.BuildingBlocks.Inbox.EFCore.Transactions;
using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Consumers;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Services;
using LucidMicro.Contracts.Notification.IntegrationEvents;
using LucidMicro.Services.Notification.Application.Features.Notifications.IntegrationEvents;
using LucidMicro.Services.Notification.Infrastructure.Persistence;
using LucidMicro.Services.Notification.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LucidMicro.Services.Notification.Infrastructure.Tests;

public sealed class NotificationInfrastructureHealthCheckTests
{
    [Fact]
    public void AddNotificationInfrastructure_RegistersPostgreSqlReadyCheck()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddNotificationInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = Assert.Single(
            options.Registrations,
            registration => registration.Name == LucidHealthCheckTags.PostgreSql);

        Assert.Contains(LucidHealthCheckTags.Ready, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.Database, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.PostgreSql, registration.Tags);
        Assert.Contains(
            options.Registrations,
            registration => registration.Name == LucidHealthCheckTags.RabbitMq);
        Assert.Contains(
            options.Registrations,
            registration => registration.Name == LucidHealthCheckTags.Consul);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IHostedService)
                       && service.ImplementationType == typeof(ConsulServiceRegistrationHostedService));
    }

    [Fact]
    public void AddNotificationInfrastructure_RegistersInboxStore()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddNotificationInfrastructure(configuration);

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IInboxMessageStore)
                       && service.ImplementationType == typeof(EfCoreInboxMessageStore<NotificationDbContext>));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IInboxProcessingTransaction)
                       && service.ImplementationType == typeof(EfCoreInboxProcessingTransaction<NotificationDbContext>));
    }

    [Fact]
    public void AddNotificationInfrastructure_RegistersRabbitMqConsumer()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddNotificationInfrastructure(configuration);

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(RabbitMqConsumerRegistration)
                       && service.ImplementationInstance is RabbitMqConsumerRegistration
                       {
                           EventType: var eventType,
                           HandlerType: var handlerType
                       }
                       && eventType == typeof(NotificationSendRequestedIntegrationEvent)
                       && handlerType == typeof(NotificationSendRequestedIntegrationEventHandler));
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Notification"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Lucid:ServiceDiscovery:Consul:Address"] = "http://consul:8500",
                ["Lucid:ServiceDiscovery:Consul:RequestTimeoutSeconds"] = "3",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceName"] = "notification",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceId"] = "notification-test",
                ["Lucid:ServiceDiscovery:Consul:Registration:Address"] = "localhost",
                ["Lucid:ServiceDiscovery:Consul:Registration:Port"] = "49853",
                ["Lucid:EventBus:RabbitMQ:ConnectionString"] = "amqp://guest:guest@localhost:5672/",
                ["Lucid:EventBus:RabbitMQ:ExchangeName"] = "lucid.events",
                ["Authentication:Jwt:Issuer"] = "LucidMicro.Identity",
                ["Authentication:Jwt:Audience"] = "LucidMicro.Admin",
                ["Authentication:Jwt:RefreshAudience"] = "LucidMicro.Admin.Refresh",
                ["Authentication:Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes"
            })
            .Build();
    }
}
