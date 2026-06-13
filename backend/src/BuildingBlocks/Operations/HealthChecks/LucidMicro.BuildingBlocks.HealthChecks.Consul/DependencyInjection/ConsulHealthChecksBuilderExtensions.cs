using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LucidMicro.BuildingBlocks.HealthChecks.Consul.DependencyInjection;

public static class ConsulHealthChecksBuilderExtensions
{
    private static readonly string[] DefaultTags =
    [
        LucidHealthCheckTags.Ready,
        LucidHealthCheckTags.ServiceDiscovery,
        LucidHealthCheckTags.Consul
    ];

    public static IServiceCollection AddLucidConsulHealthCheck(
        this IServiceCollection services,
        string name = LucidHealthCheckTags.Consul)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddLucidConsulCheck(name);

        return services;
    }

    public static IHealthChecksBuilder AddLucidConsulCheck(
        this IHealthChecksBuilder builder,
        string name = LucidHealthCheckTags.Consul)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        builder.Services.AddHttpClient(ConsulHealthCheck.HttpClientName, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<LucidConsulServiceDiscoveryOptions>();

            client.BaseAddress = new Uri(options.Address);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });

        return builder.AddCheck<ConsulHealthCheck>(
            name,
            tags: DefaultTags);
    }
}
