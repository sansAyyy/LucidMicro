using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Exceptions;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Services;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Http;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Http.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class ServiceDiscoveryHttpMessageHandlerTests
{
    [Fact]
    public async Task SendAsync_RewritesRelativeUri_UsingResolvedEndpoint()
    {
        var resolver = new StubServiceEndpointResolver(
            new Uri("http://localhost:49853"));
        var selector = new RoundRobinServiceEndpointSelector();
        var innerHandler = new CaptureHttpMessageHandler();
        var handler = new ServiceDiscoveryHttpMessageHandler("notification", resolver, selector)
        {
            InnerHandler = innerHandler
        };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/notifications"), CancellationToken.None);

        Assert.Equal(new Uri("http://localhost:49853/api/notifications"), innerHandler.RequestUri);
    }

    [Fact]
    public async Task SendAsync_DoesNotRewriteAbsoluteUri()
    {
        var resolver = new StubServiceEndpointResolver(
            new Uri("http://localhost:49853"));
        var selector = new RoundRobinServiceEndpointSelector();
        var innerHandler = new CaptureHttpMessageHandler();
        var handler = new ServiceDiscoveryHttpMessageHandler("notification", resolver, selector)
        {
            InnerHandler = innerHandler
        };
        using var invoker = new HttpMessageInvoker(handler);
        var absoluteUri = new Uri("https://example.com/api/notifications");

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, absoluteUri), CancellationToken.None);

        Assert.Equal(absoluteUri, innerHandler.RequestUri);
    }

    [Fact]
    public async Task SendAsync_RewritesServiceNamePlaceholderUri()
    {
        var resolver = new StubServiceEndpointResolver(
            new Uri("http://localhost:49853"));
        var selector = new RoundRobinServiceEndpointSelector();
        var innerHandler = new CaptureHttpMessageHandler();
        var handler = new ServiceDiscoveryHttpMessageHandler("notification", resolver, selector)
        {
            InnerHandler = innerHandler
        };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://notification/api/notifications"), CancellationToken.None);

        Assert.Equal(new Uri("http://localhost:49853/api/notifications"), innerHandler.RequestUri);
    }

    [Fact]
    public async Task SendAsync_Throws_WhenServiceHasNoEndpoints()
    {
        var resolver = new StubServiceEndpointResolver();
        var selector = new RoundRobinServiceEndpointSelector();
        var handler = new ServiceDiscoveryHttpMessageHandler("notification", resolver, selector)
        {
            InnerHandler = new CaptureHttpMessageHandler()
        };
        using var invoker = new HttpMessageInvoker(handler);

        await Assert.ThrowsAsync<ServiceEndpointNotFoundException>(() => invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "/api/notifications"),
            CancellationToken.None));
    }

    [Fact]
    public async Task AddLucidServiceDiscovery_RegistersHttpMessageHandler()
    {
        var services = new ServiceCollection();
        var innerHandler = new CaptureHttpMessageHandler();

        services.AddSingleton<IServiceEndpointResolver>(
            new StubServiceEndpointResolver(new Uri("http://localhost:49853")));
        services.AddSingleton<IServiceEndpointSelector, RoundRobinServiceEndpointSelector>();
        services
            .AddHttpClient("notification")
            .ConfigurePrimaryHttpMessageHandler(() => innerHandler)
            .AddLucidServiceDiscovery("notification");

        using var serviceProvider = services.BuildServiceProvider();
        var httpClient = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("notification");

        await httpClient.GetAsync("/api/notifications");

        Assert.Equal(new Uri("http://localhost:49853/api/notifications"), innerHandler.RequestUri);
    }

    private sealed class StubServiceEndpointResolver(params Uri[] endpoints) : IServiceEndpointResolver
    {
        public ValueTask<IReadOnlyList<Uri>> ResolveAsync(
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult((IReadOnlyList<Uri>)endpoints);
        }
    }

    private sealed class CaptureHttpMessageHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
