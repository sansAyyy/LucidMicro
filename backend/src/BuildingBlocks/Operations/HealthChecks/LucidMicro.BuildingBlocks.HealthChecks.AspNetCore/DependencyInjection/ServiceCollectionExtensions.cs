using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LucidMicro.BuildingBlocks.HealthChecks.AspNetCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHealthChecksBuilder AddLucidHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddHealthChecks();
    }
}
