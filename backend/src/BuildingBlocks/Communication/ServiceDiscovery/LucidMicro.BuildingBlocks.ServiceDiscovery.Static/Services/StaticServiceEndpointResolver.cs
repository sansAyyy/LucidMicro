using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Static.Options;

namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Static.Services;

public sealed class StaticServiceEndpointResolver : IServiceEndpointResolver
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<Uri>> _services;

    public StaticServiceEndpointResolver(LucidStaticServiceDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();
        _services = options.Services.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<Uri>)pair.Value.Select(endpoint => new Uri(endpoint)).ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<IReadOnlyList<Uri>> ResolveAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(
            _services.TryGetValue(serviceName, out var endpoints)
                ? endpoints
                : Array.Empty<Uri>());
    }
}
