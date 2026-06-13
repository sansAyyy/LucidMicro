using System.Net;
using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.HealthChecks.Consul;
using LucidMicro.BuildingBlocks.HealthChecks.Consul.DependencyInjection;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class ConsulHealthChecksBuilderExtensionsTests
{
    [Fact]
    public void AddLucidConsulCheck_RegistersReadyServiceDiscoveryCheck()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new LucidConsulServiceDiscoveryOptions
        {
            Address = "http://localhost:8500"
        });

        services.AddHealthChecks()
            .AddLucidConsulCheck();

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = Assert.Single(options.Registrations);

        Assert.Equal(LucidHealthCheckTags.Consul, registration.Name);
        Assert.Equal(typeof(ConsulHealthCheck), registration.Factory(serviceProvider).GetType());
        Assert.Contains(LucidHealthCheckTags.Ready, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.ServiceDiscovery, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.Consul, registration.Tags);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenConsulIsReachable()
    {
        var healthCheck = CreateHealthCheck(HttpStatusCode.OK);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenConsulReturnsFailure()
    {
        var healthCheck = CreateHealthCheck(HttpStatusCode.InternalServerError);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private static ConsulHealthCheck CreateHealthCheck(HttpStatusCode statusCode)
    {
        var httpClient = new HttpClient(new StatusHttpMessageHandler(statusCode))
        {
            BaseAddress = new Uri("http://consul:8500")
        };

        return new ConsulHealthCheck(
            new StubHttpClientFactory(httpClient),
            new LucidConsulServiceDiscoveryOptions
            {
                Address = "http://consul:8500"
            });
    }

    private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return httpClient;
        }
    }

    private sealed class StatusHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
