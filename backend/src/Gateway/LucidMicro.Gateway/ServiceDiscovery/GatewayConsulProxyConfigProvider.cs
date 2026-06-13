using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.Gateway.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace LucidMicro.Gateway.ServiceDiscovery;

public sealed class GatewayConsulProxyConfigProvider : IProxyConfigProvider
{
    private readonly IServiceEndpointResolver _endpointResolver;
    private readonly LucidGatewayServiceDiscoveryOptions _options;
    private readonly ILogger<GatewayConsulProxyConfigProvider> _logger;
    private readonly InMemoryConfigProvider _configProvider;
    private readonly IReadOnlyList<RouteConfig> _routes;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string _revisionKey = string.Empty;
    private int _revision;

    public GatewayConsulProxyConfigProvider(
        IConfiguration configuration,
        IServiceEndpointResolver endpointResolver,
        LucidGatewayServiceDiscoveryOptions options,
        ILogger<GatewayConsulProxyConfigProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(endpointResolver);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        options.Validate();

        _endpointResolver = endpointResolver;
        _options = options;
        _logger = logger;
        _routes = LoadRoutes(configuration.GetRequiredSection("ReverseProxy:Routes"));
        _configProvider = new InMemoryConfigProvider(
            _routes,
            LoadInitialClusters(configuration.GetRequiredSection("ReverseProxy:Clusters")));
    }

    public IProxyConfig GetConfig()
    {
        return _configProvider.GetConfig();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var clusters = new List<ClusterConfig>();
            var revisionParts = new List<string>();

            foreach (var (clusterId, serviceName) in _options.Clusters.OrderBy(cluster => cluster.Key))
            {
                var endpoints = await _endpointResolver.ResolveAsync(serviceName, cancellationToken);
                var orderedEndpoints = endpoints
                    .OrderBy(endpoint => endpoint.ToString(), StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                clusters.Add(CreateCluster(clusterId, orderedEndpoints));
                revisionParts.Add($"{clusterId}={string.Join(",", orderedEndpoints.Select(endpoint => endpoint.ToString()))}");
            }

            var revisionKey = string.Join(";", revisionParts);
            if (revisionKey == _revisionKey)
            {
                return;
            }

            _revisionKey = revisionKey;
            _revision++;
            _configProvider.Update(_routes, clusters, _revision.ToString());

            foreach (var cluster in clusters)
            {
                _logger.LogInformation(
                    "Gateway cluster {ClusterId} refreshed with {DestinationCount} destinations.",
                    cluster.ClusterId,
                    cluster.Destinations?.Count ?? 0);
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private IReadOnlyList<RouteConfig> LoadRoutes(IConfigurationSection routesSection)
    {
        var routes = routesSection.Get<Dictionary<string, RouteConfig>>()
            ?? throw new InvalidOperationException("Gateway reverse proxy routes are not configured.");

        return routes
            .Select(route => CreateRoute(route.Key, route.Value))
            .ToArray();
    }

    private IReadOnlyList<ClusterConfig> LoadInitialClusters(IConfigurationSection clustersSection)
    {
        var configuredClusters = clustersSection.Get<Dictionary<string, ClusterConfig>>()
            ?? new Dictionary<string, ClusterConfig>(StringComparer.OrdinalIgnoreCase);

        return _options.Clusters.Keys
            .Select(clusterId =>
            {
                if (configuredClusters.TryGetValue(clusterId, out var cluster))
                {
                    return CreateInitialCluster(clusterId, cluster);
                }

                return CreateCluster(clusterId, []);
            })
            .ToArray();
    }

    private static RouteConfig CreateRoute(string routeId, RouteConfig route)
    {
        return new RouteConfig
        {
            RouteId = string.IsNullOrWhiteSpace(route.RouteId) ? routeId : route.RouteId,
            Match = route.Match,
            Order = route.Order,
            ClusterId = route.ClusterId,
            AuthorizationPolicy = route.AuthorizationPolicy,
            RateLimiterPolicy = route.RateLimiterPolicy,
            OutputCachePolicy = route.OutputCachePolicy,
            TimeoutPolicy = route.TimeoutPolicy,
            Timeout = route.Timeout,
            CorsPolicy = route.CorsPolicy,
            MaxRequestBodySize = route.MaxRequestBodySize,
            Metadata = route.Metadata,
            Transforms = route.Transforms
        };
    }

    private ClusterConfig CreateInitialCluster(string clusterId, ClusterConfig cluster)
    {
        return new ClusterConfig
        {
            ClusterId = string.IsNullOrWhiteSpace(cluster.ClusterId) ? clusterId : cluster.ClusterId,
            LoadBalancingPolicy = _options.LoadBalancingPolicy,
            SessionAffinity = cluster.SessionAffinity,
            HealthCheck = cluster.HealthCheck,
            HttpClient = cluster.HttpClient,
            HttpRequest = cluster.HttpRequest,
            Destinations = cluster.Destinations,
            Metadata = cluster.Metadata
        };
    }

    private ClusterConfig CreateCluster(string clusterId, IReadOnlyList<Uri> endpoints)
    {
        return new ClusterConfig
        {
            ClusterId = clusterId,
            LoadBalancingPolicy = _options.LoadBalancingPolicy,
            Destinations = endpoints
                .Select((endpoint, index) => new KeyValuePair<string, DestinationConfig>(
                    $"destination{index + 1}",
                    new DestinationConfig
                    {
                        Address = EnsureTrailingSlash(endpoint).ToString()
                    }))
                .ToDictionary(
                    destination => destination.Key,
                    destination => destination.Value,
                    StringComparer.OrdinalIgnoreCase)
        };
    }

    private static Uri EnsureTrailingSlash(Uri endpoint)
    {
        if (endpoint.AbsolutePath == "/")
        {
            return endpoint;
        }

        var builder = new UriBuilder(endpoint)
        {
            Path = "/"
        };

        return builder.Uri;
    }
}
