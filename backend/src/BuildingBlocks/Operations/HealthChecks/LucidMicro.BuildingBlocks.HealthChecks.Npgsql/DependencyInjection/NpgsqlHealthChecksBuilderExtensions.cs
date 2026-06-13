using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.HealthChecks.Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LucidMicro.BuildingBlocks.HealthChecks.Npgsql.DependencyInjection;

public static class NpgsqlHealthChecksBuilderExtensions
{
    private static readonly string[] DefaultTags =
    [
        LucidHealthCheckTags.Ready,
        LucidHealthCheckTags.Database,
        LucidHealthCheckTags.PostgreSql
    ];

    public static IServiceCollection AddLucidNpgsqlDbContextHealthCheck<TDbContext>(
        this IServiceCollection services,
        string name = LucidHealthCheckTags.PostgreSql)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddLucidNpgsqlDbContextCheck<TDbContext>(name);

        return services;
    }

    public static IHealthChecksBuilder AddLucidNpgsqlDbContextCheck<TDbContext>(
        this IHealthChecksBuilder builder,
        string name = LucidHealthCheckTags.PostgreSql)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return builder.AddCheck<NpgsqlDbContextHealthCheck<TDbContext>>(
            name,
            tags: DefaultTags);
    }
}
