using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Serialization;
using LucidMicro.BuildingBlocks.Outbox.Abstractions.Services;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Stores;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Services;
using LucidMicro.Services.Identity.Infrastructure.DependencyInjection;
using LucidMicro.Services.Identity.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LucidMicro.Services.Identity.Infrastructure.Tests;

public sealed class IdentityInfrastructureHealthCheckTests
{
    [Fact]
    public void AddIdentityInfrastructure_RegistersPostgreSqlReadyCheck()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddIdentityInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = Assert.Single(
            options.Registrations,
            registration => registration.Name == LucidHealthCheckTags.PostgreSql);

        Assert.Contains(LucidHealthCheckTags.Ready, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.Database, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.PostgreSql, registration.Tags);
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersOutboxInfrastructure()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddIdentityInfrastructure(configuration);

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IOutboxMessageStore)
                       && service.ImplementationType == typeof(EfCoreOutboxMessageStore<IdentityDbContext>));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IOutboxEventWriter)
                       && service.ImplementationType == typeof(DefaultOutboxEventWriter));
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IOutboxMessageSerializer)
                       && service.ImplementationType == typeof(SystemTextJsonOutboxMessageSerializer));
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersAuthInfrastructure()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddIdentityInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<IPasswordHashingService>());
        Assert.NotNull(serviceProvider.GetRequiredService<ICurrentUser>());
        Assert.NotNull(serviceProvider.GetRequiredService<IAccessTokenService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IRefreshTokenService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IRefreshTokenValidator>());
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersMessagingInfrastructure()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddIdentityInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var healthOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IHostedService));
        Assert.Contains(
            healthOptions.Registrations,
            registration => registration.Name == LucidHealthCheckTags.RabbitMq);
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersConsulReadyCheck()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddIdentityInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var healthOptions = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = Assert.Single(
            healthOptions.Registrations,
            registration => registration.Name == LucidHealthCheckTags.Consul);

        Assert.Contains(LucidHealthCheckTags.Ready, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.ServiceDiscovery, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.Consul, registration.Tags);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IHostedService)
                       && service.ImplementationType == typeof(ConsulServiceRegistrationHostedService));
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Lucid:Caching:Redis:ConnectionString"] = "localhost:6379",
                ["Lucid:Identity:SmsLogin:CodeTtlSeconds"] = "300",
                ["Lucid:Identity:SmsLogin:SendIntervalSeconds"] = "60",
                ["Lucid:Identity:SmsLogin:AttemptTtlSeconds"] = "300",
                ["Lucid:Identity:SmsLogin:MaxAttempts"] = "5",
                ["Lucid:Resilience:Http:Enabled"] = "false",
                ["Lucid:ServiceDiscovery:Consul:Address"] = "http://consul:8500",
                ["Lucid:ServiceDiscovery:Consul:RequestTimeoutSeconds"] = "3",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceName"] = "identity",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceId"] = "identity-test",
                ["Lucid:ServiceDiscovery:Consul:Registration:Address"] = "localhost",
                ["Lucid:ServiceDiscovery:Consul:Registration:Port"] = "49753",
                ["Lucid:EventBus:RabbitMQ:ConnectionString"] = "amqp://guest:guest@localhost:5672/",
                ["Lucid:EventBus:RabbitMQ:ExchangeName"] = "lucid.events",
                ["Authentication:Jwt:Issuer"] = "LucidMicro.Identity",
                ["Authentication:Jwt:Audience"] = "LucidMicro.Admin",
                ["Authentication:Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes"
            })
            .Build();
    }
}
