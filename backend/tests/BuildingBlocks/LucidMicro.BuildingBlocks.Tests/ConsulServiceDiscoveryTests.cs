using System.Net;
using System.Text.Json;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Services;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.DependencyInjection;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Services;
using LucidMicro.Tests.Shared.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class ConsulServiceDiscoveryTests
{
    [Fact]
    public async Task ResolveAsync_ReturnsEndpoints_FromConsulHealthApi()
    {
        using var httpClient = CreateHttpClient(
            """
            [
              {
                "Node": { "Address": "10.0.0.12" },
                "Service": {
                  "Address": "10.0.0.13",
                  "Port": 49853,
                  "Meta": { "scheme": "https" }
                }
              },
              {
                "Node": { "Address": "10.0.0.14" },
                "Service": {
                  "Address": "",
                  "Port": 49854,
                  "Meta": {}
                }
              }
            ]
            """,
            out var handler);
        var resolver = new ConsulServiceEndpointResolver(
            httpClient,
            new LucidConsulServiceDiscoveryOptions
            {
                Address = "http://consul:8500",
                Datacenter = "dc1",
                Token = "test-token"
            });

        var endpoints = await resolver.ResolveAsync("notification");

        Assert.Equal(
            [
                new Uri("https://10.0.0.13:49853"),
                new Uri("http://10.0.0.14:49854")
            ],
            endpoints);
        Assert.Equal(new Uri("http://consul:8500/v1/health/service/notification?passing=true&dc=dc1"), handler.RequestUri);
        Assert.True(handler.TokenWasSent);
    }

    [Fact]
    public async Task ResolveAsync_CachesEndpoints_DuringCacheDuration()
    {
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero));
        using var httpClient = CreateHttpClient(
            """
            [
              {
                "Node": { "Address": "10.0.0.12" },
                "Service": {
                  "Address": "",
                  "Port": 49853,
                  "Meta": {}
                }
              }
            ]
            """,
            out var handler);
        var resolver = new ConsulServiceEndpointResolver(
            httpClient,
            new LucidConsulServiceDiscoveryOptions
            {
                Address = "http://consul:8500",
                CacheDurationSeconds = 10
            },
            timeProvider);

        var first = await resolver.ResolveAsync("notification");
        var second = await resolver.ResolveAsync("notification");

        Assert.Equal(first, second);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task ResolveAsync_RefreshesCache_WhenCacheExpires()
    {
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero));
        using var httpClient = CreateHttpClient(
            """
            [
              {
                "Node": { "Address": "10.0.0.12" },
                "Service": {
                  "Address": "",
                  "Port": 49853,
                  "Meta": {}
                }
              }
            ]
            """,
            out var handler);
        var resolver = new ConsulServiceEndpointResolver(
            httpClient,
            new LucidConsulServiceDiscoveryOptions
            {
                Address = "http://consul:8500",
                CacheDurationSeconds = 10
            },
            timeProvider);

        await resolver.ResolveAsync("notification");
        timeProvider.UtcNow = timeProvider.UtcNow.AddSeconds(11);
        await resolver.ResolveAsync("notification");

        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public void AddLucidConsulServiceDiscovery_RegistersResolverSelectorAndOptions()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:ServiceDiscovery:Consul:Address"] = "http://localhost:8500",
            ["Lucid:ServiceDiscovery:Consul:Datacenter"] = "dc1",
            ["Lucid:ServiceDiscovery:Consul:OnlyPassing"] = "true",
            ["Lucid:ServiceDiscovery:Consul:CacheDurationSeconds"] = "15",
            ["Lucid:ServiceDiscovery:Consul:RequestTimeoutSeconds"] = "3"
        });
        var services = new ServiceCollection();

        services.AddLucidConsulServiceDiscovery(
            configuration.GetRequiredSection(LucidConsulServiceDiscoveryOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<LucidConsulServiceDiscoveryOptions>>().Value;

        Assert.Equal("http://localhost:8500", options.Address);
        Assert.Equal("dc1", options.Datacenter);
        Assert.Equal(15, options.CacheDurationSeconds);
        Assert.Equal(3, options.RequestTimeoutSeconds);
        Assert.Equal(
            TimeSpan.FromSeconds(3),
            serviceProvider
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient(ConsulServiceEndpointResolver.HttpClientName)
                .Timeout);
        Assert.IsType<ConsulServiceEndpointResolver>(
            serviceProvider.GetRequiredService<IServiceEndpointResolver>());
        Assert.IsType<RoundRobinServiceEndpointSelector>(
            serviceProvider.GetRequiredService<IServiceEndpointSelector>());
    }

    [Fact]
    public void AddLucidConsulServiceDiscovery_Throws_WhenAddressIsInvalid()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:ServiceDiscovery:Consul:Address"] = "localhost:8500"
        });
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddLucidConsulServiceDiscovery(
            configuration.GetRequiredSection(LucidConsulServiceDiscoveryOptions.ConfigurationSectionName)));
    }

    [Fact]
    public void AddLucidConsulServiceDiscovery_Throws_WhenRequestTimeoutIsInvalid()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:ServiceDiscovery:Consul:Address"] = "http://localhost:8500",
            ["Lucid:ServiceDiscovery:Consul:RequestTimeoutSeconds"] = "0"
        });
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddLucidConsulServiceDiscovery(
            configuration.GetRequiredSection(LucidConsulServiceDiscoveryOptions.ConfigurationSectionName)));
    }

    [Fact]
    public async Task ResolveAsync_DoesNotCacheEmptyEndpoints()
    {
        using var httpClient = CreateHttpClient("[]", out var handler);
        var resolver = new ConsulServiceEndpointResolver(
            httpClient,
            new LucidConsulServiceDiscoveryOptions
            {
                Address = "http://consul:8500",
                CacheDurationSeconds = 10
            });

        var first = await resolver.ResolveAsync("notification");
        var second = await resolver.ResolveAsync("notification");

        Assert.Empty(first);
        Assert.Empty(second);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task ResolveAsync_Throws_WhenConsulReturnsFailure()
    {
        using var httpClient = CreateHttpClient(
            "failure",
            out _,
            HttpStatusCode.InternalServerError);
        var resolver = new ConsulServiceEndpointResolver(
            httpClient,
            new LucidConsulServiceDiscoveryOptions
            {
                Address = "http://consul:8500"
            });

        await Assert.ThrowsAsync<HttpRequestException>(() => resolver
            .ResolveAsync("notification")
            .AsTask());
    }

    [Fact]
    public async Task ResolveAsync_Throws_WhenConsulReturnsInvalidJson()
    {
        using var httpClient = CreateHttpClient("{", out _);
        var resolver = new ConsulServiceEndpointResolver(
            httpClient,
            new LucidConsulServiceDiscoveryOptions
            {
                Address = "http://consul:8500"
            });

        await Assert.ThrowsAsync<JsonException>(() => resolver
            .ResolveAsync("notification")
            .AsTask());
    }

    private static HttpClient CreateHttpClient(
        string responseBody,
        out CaptureHttpMessageHandler handler,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        handler = new CaptureHttpMessageHandler(responseBody, statusCode);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://consul:8500")
        };
    }

    private static IConfigurationRoot CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class CaptureHttpMessageHandler(
        string responseBody,
        HttpStatusCode statusCode) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        public int RequestCount { get; private set; }

        public bool TokenWasSent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestCount++;
            TokenWasSent = request.Headers.Contains("X-Consul-Token");

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
