using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ.DependencyInjection;

public static class RabbitMqHealthChecksBuilderExtensions
{
    private static readonly string[] DefaultTags =
    [
        LucidHealthCheckTags.Ready,
        LucidHealthCheckTags.Messaging,
        LucidHealthCheckTags.RabbitMq
    ];

    public static IServiceCollection AddLucidRabbitMqHealthCheck(
        this IServiceCollection services,
        string name = LucidHealthCheckTags.RabbitMq)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddLucidRabbitMqCheck(name);

        return services;
    }

    public static IHealthChecksBuilder AddLucidRabbitMqCheck(
        this IHealthChecksBuilder builder,
        string name = LucidHealthCheckTags.RabbitMq)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return builder.AddCheck<RabbitMqHealthCheck>(
            name,
            tags: DefaultTags);
    }
}
