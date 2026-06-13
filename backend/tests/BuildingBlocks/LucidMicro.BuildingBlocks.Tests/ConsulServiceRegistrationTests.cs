using System.Net;
using System.Text.Json;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.DependencyInjection;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class ConsulServiceRegistrationTests
{
    [Fact]
    public void Options_Validate_Throws_WhenServiceNameIsMissing()
    {
        var options = CreateValidOptions();
        options.ServiceName = string.Empty;

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Options_Validate_Throws_WhenPortIsInvalid()
    {
        var options = CreateValidOptions();
        options.Port = 0;

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Options_Validate_AllowsMissingServiceIdAndAddress_WhenUsingInstanceDefaults()
    {
        var options = CreateValidOptions();
        options.ServiceId = string.Empty;
        options.Address = string.Empty;
        options.UseInstanceDefaults = true;

        options.Validate();
    }

    [Fact]
    public void AddLucidConsulServiceRegistration_RegistersHostedServiceAndOptions()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();

        services.AddLucidConsulServiceDiscovery(
            configuration.GetRequiredSection(LucidConsulServiceDiscoveryOptions.ConfigurationSectionName));
        services.AddLucidConsulServiceRegistration(
            configuration.GetRequiredSection(LucidConsulServiceRegistrationOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<LucidConsulServiceRegistrationOptions>>().Value;

        Assert.Equal("notification", options.ServiceName);
        Assert.Equal("notification-local", options.ServiceId);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IHostedService)
                       && service.ImplementationType == typeof(ConsulServiceRegistrationHostedService));
    }

    [Fact]
    public async Task StartAsync_RegistersService_WithConsulAgent()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.OK, out var handler);
        var hostedService = CreateHostedService(httpClient);

        await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(HttpMethod.Put, handler.Requests.Single().Method);
        Assert.Equal(
            new Uri("http://consul:8500/v1/agent/service/register"),
            handler.Requests.Single().RequestUri);
        Assert.True(handler.Requests.Single().TokenWasSent);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body!);
        Assert.Equal("notification-local", body.RootElement.GetProperty("ID").GetString());
        Assert.Equal("notification", body.RootElement.GetProperty("Name").GetString());
        Assert.Equal("localhost", body.RootElement.GetProperty("Address").GetString());
        Assert.Equal(49853, body.RootElement.GetProperty("Port").GetInt32());
        Assert.Equal("http", body.RootElement.GetProperty("Meta").GetProperty("scheme").GetString());
        Assert.Equal(
            "http://localhost:49853/ready",
            body.RootElement.GetProperty("Check").GetProperty("HTTP").GetString());
        Assert.Equal("10s", body.RootElement.GetProperty("Check").GetProperty("Interval").GetString());
        Assert.Equal(
            "60s",
            body.RootElement.GetProperty("Check").GetProperty("DeregisterCriticalServiceAfter").GetString());
    }

    [Fact]
    public async Task StartAsync_UsesInstanceDefaults_WhenConfigured()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.OK, out var handler);
        var options = CreateValidOptions();
        options.ServiceId = string.Empty;
        options.Address = string.Empty;
        options.UseInstanceDefaults = true;
        var hostedService = CreateHostedService(httpClient, options);

        await hostedService.StartAsync(CancellationToken.None);

        using var body = JsonDocument.Parse(handler.Requests.Single().Body!);
        var serviceId = body.RootElement.GetProperty("ID").GetString();
        var address = body.RootElement.GetProperty("Address").GetString();
        var healthCheckUrl = body.RootElement.GetProperty("Check").GetProperty("HTTP").GetString();

        Assert.Equal($"notification-{Environment.MachineName}", serviceId);
        Assert.False(string.IsNullOrWhiteSpace(address));
        Assert.False(string.IsNullOrWhiteSpace(healthCheckUrl));
        Assert.Contains(":49853/ready", healthCheckUrl);
    }

    [Fact]
    public async Task StopAsync_DeregistersService_WithConsulAgent()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.OK, out var handler);
        var hostedService = CreateHostedService(httpClient);

        await hostedService.StopAsync(CancellationToken.None);

        Assert.Equal(HttpMethod.Put, handler.Requests.Single().Method);
        Assert.Equal(
            new Uri("http://consul:8500/v1/agent/service/deregister/notification-local"),
            handler.Requests.Single().RequestUri);
        Assert.True(handler.Requests.Single().TokenWasSent);
    }

    [Fact]
    public async Task StartAsync_Throws_WhenConsulReturnsFailure()
    {
        using var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError, out _);
        var hostedService = CreateHostedService(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => hostedService.StartAsync(CancellationToken.None));
    }

    private static ConsulServiceRegistrationHostedService CreateHostedService(
        HttpClient httpClient,
        LucidConsulServiceRegistrationOptions? options = null)
    {
        return new ConsulServiceRegistrationHostedService(
            new StubHttpClientFactory(httpClient),
            new LucidConsulServiceDiscoveryOptions
            {
                Address = "http://consul:8500",
                Token = "test-token"
            },
            options ?? CreateValidOptions());
    }

    private static HttpClient CreateHttpClient(
        HttpStatusCode statusCode,
        out CaptureHttpMessageHandler handler)
    {
        handler = new CaptureHttpMessageHandler(statusCode);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://consul:8500")
        };
    }

    private static LucidConsulServiceRegistrationOptions CreateValidOptions()
    {
        return new LucidConsulServiceRegistrationOptions
        {
            ServiceName = "notification",
            ServiceId = "notification-local",
            Address = "localhost",
            Port = 49853,
            Scheme = "http",
            HealthCheckPath = "/ready",
            HealthCheckIntervalSeconds = 10,
            DeregisterCriticalServiceAfterSeconds = 60
        };
    }

    private static IConfigurationRoot CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lucid:ServiceDiscovery:Consul:Address"] = "http://consul:8500",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceName"] = "notification",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceId"] = "notification-local",
                ["Lucid:ServiceDiscovery:Consul:Registration:Address"] = "localhost",
                ["Lucid:ServiceDiscovery:Consul:Registration:Port"] = "49853"
            })
            .Build();
    }

    private sealed class StubHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return httpClient;
        }
    }

    private sealed class CaptureHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri,
                request.Headers.Contains("X-Consul-Token"),
                request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken)));

            return new HttpResponseMessage(statusCode);
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri? RequestUri,
        bool TokenWasSent,
        string? Body);
}
