namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;

public interface IServiceEndpointResolver
{
    ValueTask<IReadOnlyList<Uri>> ResolveAsync(
        string serviceName,
        CancellationToken cancellationToken = default);
}
