using System.Net;
using System.Text.Json;
using LucidMicro.Services.Identity.Api.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LucidMicro.Services.Identity.Api.Tests;

public sealed class HealthChecksHttpContractTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/live")]
    [InlineData("/ready")]
    public async Task HealthCheck_ReturnsHealthyResponse(string path)
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        Assert.True(json.RootElement.TryGetProperty("totalDuration", out _));
        Assert.True(json.RootElement.TryGetProperty("entries", out _));
    }

    [Fact]
    public async Task HealthCheck_DoesNotExposeExceptionMessage()
    {
        await using var factory = new TestApiFactory(options =>
        {
            options.Registrations.Clear();
            options.Registrations.Add(new HealthCheckRegistration(
                "failing",
                _ => new FailingHealthCheck(),
                HealthStatus.Unhealthy,
                tags: null));
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entry = json.RootElement.GetProperty("entries").GetProperty("failing");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", entry.GetProperty("status").GetString());
        Assert.False(entry.TryGetProperty("exception", out _));
    }

    private sealed class TestApiFactory : WebApplicationFactory<AdminAuthController>
    {
        private readonly Action<HealthCheckServiceOptions>? _configureHealthChecks;

        public TestApiFactory(Action<HealthCheckServiceOptions>? configureHealthChecks = null)
        {
            _configureHealthChecks = configureHealthChecks;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.Configure<HealthCheckServiceOptions>(options =>
                {
                    if (_configureHealthChecks is null)
                    {
                        options.Registrations.Clear();
                        return;
                    }

                    _configureHealthChecks(options);
                });
            });
        }
    }

    private sealed class FailingHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "failing check",
                new InvalidOperationException("sensitive failure details")));
        }
    }
}
