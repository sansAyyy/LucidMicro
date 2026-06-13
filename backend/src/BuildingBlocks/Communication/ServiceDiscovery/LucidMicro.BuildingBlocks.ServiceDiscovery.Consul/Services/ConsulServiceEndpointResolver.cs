using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;

namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Services;

public sealed class ConsulServiceEndpointResolver : IServiceEndpointResolver
{
    public const string HttpClientName = "LucidServiceDiscoveryConsul";

    private readonly HttpClient _httpClient;
    private readonly LucidConsulServiceDiscoveryOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ConsulServiceEndpointResolver(
        HttpClient httpClient,
        LucidConsulServiceDiscoveryOptions options)
        : this(httpClient, options, TimeProvider.System)
    {
    }

    public ConsulServiceEndpointResolver(
        HttpClient httpClient,
        LucidConsulServiceDiscoveryOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        options.Validate();

        _httpClient = httpClient;
        _options = options;
        _timeProvider = timeProvider;
    }

    public async ValueTask<IReadOnlyList<Uri>> ResolveAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var cached = await TryGetCachedAsync(serviceName, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var endpoints = await QueryEndpointsAsync(serviceName, cancellationToken);

        if (endpoints.Count > 0)
        {
            await SetCachedAsync(serviceName, endpoints, cancellationToken);
        }

        return endpoints;
    }

    private async Task<IReadOnlyList<Uri>?> TryGetCachedAsync(
        string serviceName,
        CancellationToken cancellationToken)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(serviceName, out var entry)
                && entry.ExpiresAt > _timeProvider.GetUtcNow())
            {
                return entry.Endpoints;
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        return null;
    }

    private async Task SetCachedAsync(
        string serviceName,
        IReadOnlyList<Uri> endpoints,
        CancellationToken cancellationToken)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _cache[serviceName] = new CacheEntry(
                endpoints,
                _timeProvider.GetUtcNow().AddSeconds(_options.CacheDurationSeconds));
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task<IReadOnlyList<Uri>> QueryEndpointsAsync(
        string serviceName,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildHealthServicePath(serviceName));

        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            request.Headers.Add("X-Consul-Token", _options.Token);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var services = await response.Content.ReadFromJsonAsync<ConsulHealthServiceResponse[]>(
            cancellationToken: cancellationToken);

        if (services is null || services.Length == 0)
        {
            return Array.Empty<Uri>();
        }

        return services
            .Select(CreateEndpoint)
            .Where(endpoint => endpoint is not null)
            .Select(endpoint => endpoint!)
            .Distinct()
            .ToArray();
    }

    private string BuildHealthServicePath(string serviceName)
    {
        var path = $"/v1/health/service/{Uri.EscapeDataString(serviceName)}";
        var query = _options.OnlyPassing ? "?passing=true" : string.Empty;

        if (!string.IsNullOrWhiteSpace(_options.Datacenter))
        {
            query += string.IsNullOrEmpty(query)
                ? $"?dc={Uri.EscapeDataString(_options.Datacenter)}"
                : $"&dc={Uri.EscapeDataString(_options.Datacenter)}";
        }

        return path + query;
    }

    private static Uri? CreateEndpoint(ConsulHealthServiceResponse response)
    {
        if (response.Service is null)
        {
            return null;
        }

        var host = !string.IsNullOrWhiteSpace(response.Service.Address)
            ? response.Service.Address
            : response.Node?.Address;
        if (string.IsNullOrWhiteSpace(host) || response.Service.Port <= 0)
        {
            return null;
        }

        var scheme = GetScheme(response.Service.Meta);
        var uriBuilder = new UriBuilder(scheme, host, response.Service.Port);

        return uriBuilder.Uri;
    }

    private static string GetScheme(IReadOnlyDictionary<string, string>? meta)
    {
        if (meta is not null
            && meta.TryGetValue("scheme", out var scheme)
            && (scheme == Uri.UriSchemeHttp || scheme == Uri.UriSchemeHttps))
        {
            return scheme;
        }

        return Uri.UriSchemeHttp;
    }

    private sealed record CacheEntry(
        IReadOnlyList<Uri> Endpoints,
        DateTimeOffset ExpiresAt);

    private sealed class ConsulHealthServiceResponse
    {
        [JsonPropertyName("Node")]
        public ConsulNode? Node { get; set; }

        [JsonPropertyName("Service")]
        public ConsulService? Service { get; set; }
    }

    private sealed class ConsulNode
    {
        [JsonPropertyName("Address")]
        public string Address { get; set; } = string.Empty;
    }

    private sealed class ConsulService
    {
        [JsonPropertyName("Address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("Port")]
        public int Port { get; set; }

        [JsonPropertyName("Meta")]
        public Dictionary<string, string> Meta { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
