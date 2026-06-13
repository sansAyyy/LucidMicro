using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LucidMicro.BuildingBlocks.HealthChecks.Consul;

public sealed class ConsulHealthCheck : IHealthCheck
{
    public const string HttpClientName = "LucidHealthChecksConsul";

    private readonly HttpClient _httpClient;

    public ConsulHealthCheck(
        IHttpClientFactory httpClientFactory,
        LucidConsulServiceDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _httpClient = httpClientFactory.CreateClient(HttpClientName);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/v1/status/leader", cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Consul is reachable.")
                : HealthCheckResult.Unhealthy($"Consul returned HTTP status code {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Consul health check failed.", exception);
        }
    }
}
