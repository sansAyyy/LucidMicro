using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Exceptions;

namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Http;

public sealed class ServiceDiscoveryHttpMessageHandler : DelegatingHandler
{
    private readonly string _serviceName;
    private readonly IServiceEndpointResolver _resolver;
    private readonly IServiceEndpointSelector _selector;

    public ServiceDiscoveryHttpMessageHandler(
        string serviceName,
        IServiceEndpointResolver resolver,
        IServiceEndpointSelector selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(selector);

        _serviceName = serviceName;
        _resolver = resolver;
        _selector = selector;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RequestUri is null)
        {
            throw new InvalidOperationException("HTTP request URI is required for service discovery.");
        }

        if (request.RequestUri.IsAbsoluteUri
            && !IsServiceDiscoveryPlaceholder(request.RequestUri))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var endpoints = await _resolver.ResolveAsync(_serviceName, cancellationToken);
        if (endpoints.Count == 0)
        {
            throw new ServiceEndpointNotFoundException(_serviceName);
        }

        var endpoint = _selector.Select(_serviceName, endpoints);
        var relativeUri = request.RequestUri.IsAbsoluteUri
            ? new Uri(request.RequestUri.PathAndQuery, UriKind.Relative)
            : request.RequestUri;
        request.RequestUri = new Uri(endpoint, relativeUri);

        return await base.SendAsync(request, cancellationToken);
    }

    private bool IsServiceDiscoveryPlaceholder(Uri requestUri)
    {
        return string.Equals(requestUri.Host, _serviceName, StringComparison.OrdinalIgnoreCase);
    }
}
