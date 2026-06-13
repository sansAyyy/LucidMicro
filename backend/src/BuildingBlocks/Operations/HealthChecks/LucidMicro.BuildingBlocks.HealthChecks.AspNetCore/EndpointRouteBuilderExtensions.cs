using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLucidHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health", CreateOptions(_ => true));
        endpoints.MapHealthChecks("/live", CreateOptions(_ => false));
        endpoints.MapHealthChecks(
            "/ready",
            CreateOptions(registration => registration.Tags.Contains(LucidHealthCheckTags.Ready)));

        return endpoints;
    }

    private static HealthCheckOptions CreateOptions(Func<HealthCheckRegistration, bool> predicate)
    {
        return new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = WriteResponseAsync
        };
    }

    private static Task WriteResponseAsync(HttpContext httpContext, HealthReport report)
    {
        var response = new LucidHealthCheckResponse(
            report.Status.ToString(),
            report.TotalDuration,
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new LucidHealthCheckEntry(
                    entry.Value.Status.ToString(),
                    entry.Value.Description,
                    entry.Value.Duration,
                    entry.Value.Data.ToDictionary(data => data.Key, data => data.Value?.ToString()))));

        return httpContext.Response.WriteAsJsonAsync(response);
    }

    private sealed record LucidHealthCheckResponse(
        string Status,
        TimeSpan TotalDuration,
        IReadOnlyDictionary<string, LucidHealthCheckEntry> Entries);

    private sealed record LucidHealthCheckEntry(
        string Status,
        string? Description,
        TimeSpan Duration,
        IReadOnlyDictionary<string, string?> Data);
}
