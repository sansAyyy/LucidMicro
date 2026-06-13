using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LucidMicro.BuildingBlocks.HealthChecks.Redis.DependencyInjection;

public static class RedisHealthChecksBuilderExtensions
{
    private static readonly string[] DefaultTags =
    [
        LucidHealthCheckTags.Ready,
        LucidHealthCheckTags.Cache,
        LucidHealthCheckTags.Redis
    ];

    public static IServiceCollection AddLucidRedisHealthCheck(
        this IServiceCollection services,
        string name = LucidHealthCheckTags.Redis)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddLucidRedisCheck(name);

        return services;
    }

    public static IHealthChecksBuilder AddLucidRedisCheck(
        this IHealthChecksBuilder builder,
        string name = LucidHealthCheckTags.Redis)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return builder.AddCheck<RedisHealthCheck>(
            name,
            tags: DefaultTags);
    }
}
