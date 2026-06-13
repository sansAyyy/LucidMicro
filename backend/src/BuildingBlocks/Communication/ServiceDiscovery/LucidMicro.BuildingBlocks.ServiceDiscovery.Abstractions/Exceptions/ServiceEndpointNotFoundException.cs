namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Exceptions;

public sealed class ServiceEndpointNotFoundException : InvalidOperationException
{
    public ServiceEndpointNotFoundException(string serviceName)
        : base($"Service endpoint was not found for service '{serviceName}'.")
    {
        ServiceName = serviceName;
    }

    public string ServiceName { get; }
}
