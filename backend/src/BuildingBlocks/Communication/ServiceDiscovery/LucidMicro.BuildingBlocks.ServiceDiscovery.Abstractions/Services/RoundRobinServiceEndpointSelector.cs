using System.Collections.Concurrent;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Exceptions;

namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Services;

public sealed class RoundRobinServiceEndpointSelector : IServiceEndpointSelector
{
    private readonly ConcurrentDictionary<string, int> _positions = new(StringComparer.OrdinalIgnoreCase);

    public Uri Select(string serviceName, IReadOnlyList<Uri> endpoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentNullException.ThrowIfNull(endpoints);

        if (endpoints.Count == 0)
        {
            throw new ServiceEndpointNotFoundException(serviceName);
        }

        var next = _positions.AddOrUpdate(
            serviceName,
            0,
            (_, current) => unchecked(current + 1));

        var index = Math.Abs(next % endpoints.Count);

        return endpoints[index];
    }
}
