namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;

public interface IServiceEndpointSelector
{
    Uri Select(string serviceName, IReadOnlyList<Uri> endpoints);
}
