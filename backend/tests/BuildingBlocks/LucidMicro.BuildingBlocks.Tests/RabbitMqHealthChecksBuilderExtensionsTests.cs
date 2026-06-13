using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Options;
using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ;
using LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class RabbitMqHealthChecksBuilderExtensionsTests
{
    [Fact]
    public void AddLucidRabbitMqCheck_RegistersReadyMessagingCheck()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new RabbitMqEventBusOptions
        {
            ConnectionString = "amqp://guest:guest@localhost:5672/"
        });

        services.AddHealthChecks()
            .AddLucidRabbitMqCheck();

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = Assert.Single(options.Registrations);

        Assert.Equal(LucidHealthCheckTags.RabbitMq, registration.Name);
        Assert.Equal(typeof(RabbitMqHealthCheck), registration.Factory(serviceProvider).GetType());
        Assert.Contains(LucidHealthCheckTags.Ready, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.Messaging, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.RabbitMq, registration.Tags);
    }
}
