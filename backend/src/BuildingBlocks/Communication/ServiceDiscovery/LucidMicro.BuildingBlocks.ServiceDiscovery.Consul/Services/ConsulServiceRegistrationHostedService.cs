using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using Microsoft.Extensions.Hosting;

namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Services;

public sealed class ConsulServiceRegistrationHostedService : IHostedService
{
    public const string HttpClientName = "LucidServiceDiscoveryConsulRegistration";

    private readonly HttpClient _httpClient;
    private readonly LucidConsulServiceDiscoveryOptions _discoveryOptions;
    private readonly LucidConsulServiceRegistrationOptions _registrationOptions;
    private readonly string _serviceId;
    private readonly string _serviceAddress;

    public ConsulServiceRegistrationHostedService(
        IHttpClientFactory httpClientFactory,
        LucidConsulServiceDiscoveryOptions discoveryOptions,
        LucidConsulServiceRegistrationOptions registrationOptions)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(discoveryOptions);
        ArgumentNullException.ThrowIfNull(registrationOptions);

        discoveryOptions.Validate();
        registrationOptions.Validate();

        _httpClient = httpClientFactory.CreateClient(HttpClientName);
        _discoveryOptions = discoveryOptions;
        _registrationOptions = registrationOptions;
        _serviceId = ResolveServiceId(registrationOptions);
        _serviceAddress = ResolveServiceAddress(registrationOptions);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            "/v1/agent/service/register")
        {
            Content = JsonContent.Create(CreateRegistrationRequest())
        };

        AddConsulToken(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/v1/agent/service/deregister/{Uri.EscapeDataString(_serviceId)}");

        AddConsulToken(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void AddConsulToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_discoveryOptions.Token))
        {
            request.Headers.Add("X-Consul-Token", _discoveryOptions.Token);
        }
    }

    private ConsulServiceRegistrationRequest CreateRegistrationRequest()
    {
        return new ConsulServiceRegistrationRequest
        {
            Id = _serviceId,
            Name = _registrationOptions.ServiceName,
            Address = _serviceAddress,
            Port = _registrationOptions.Port,
            Meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["scheme"] = _registrationOptions.Scheme
            },
            Check = new ConsulServiceHealthCheck
            {
                Http = CreateHealthCheckUrl(),
                Interval = $"{_registrationOptions.HealthCheckIntervalSeconds}s",
                DeregisterCriticalServiceAfter = $"{_registrationOptions.DeregisterCriticalServiceAfterSeconds}s"
            }
        };
    }

    private string CreateHealthCheckUrl()
    {
        var uriBuilder = new UriBuilder(
            _registrationOptions.Scheme,
            _serviceAddress,
            _registrationOptions.Port,
            _registrationOptions.HealthCheckPath);

        return uriBuilder.Uri.ToString();
    }

    private static string ResolveServiceId(LucidConsulServiceRegistrationOptions options)
    {
        if (!options.UseInstanceDefaults && !string.IsNullOrWhiteSpace(options.ServiceId))
        {
            return options.ServiceId;
        }

        return $"{options.ServiceName}-{Environment.MachineName}";
    }

    private static string ResolveServiceAddress(LucidConsulServiceRegistrationOptions options)
    {
        if (!options.UseInstanceDefaults && !string.IsNullOrWhiteSpace(options.Address))
        {
            return options.Address;
        }

        var hostName = Dns.GetHostName();
        var addresses = Dns.GetHostAddresses(hostName);
        var address = addresses.FirstOrDefault(address =>
            address.AddressFamily == AddressFamily.InterNetwork
            && !IPAddress.IsLoopback(address));

        if (address is null)
        {
            address = addresses.FirstOrDefault(address => !IPAddress.IsLoopback(address));
        }

        return address?.ToString()
            ?? throw new InvalidOperationException("Consul service registration address could not be resolved.");
    }

    private sealed class ConsulServiceRegistrationRequest
    {
        [JsonPropertyName("ID")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("Name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("Address")]
        public string Address { get; init; } = string.Empty;

        [JsonPropertyName("Port")]
        public int Port { get; init; }

        [JsonPropertyName("Meta")]
        public Dictionary<string, string> Meta { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("Check")]
        public ConsulServiceHealthCheck Check { get; init; } = new();
    }

    private sealed class ConsulServiceHealthCheck
    {
        [JsonPropertyName("HTTP")]
        public string Http { get; init; } = string.Empty;

        [JsonPropertyName("Interval")]
        public string Interval { get; init; } = string.Empty;

        [JsonPropertyName("DeregisterCriticalServiceAfter")]
        public string DeregisterCriticalServiceAfter { get; init; } = string.Empty;
    }
}
