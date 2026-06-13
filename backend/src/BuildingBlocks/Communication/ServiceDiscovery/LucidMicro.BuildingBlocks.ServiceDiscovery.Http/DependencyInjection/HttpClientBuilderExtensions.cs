using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Http.DependencyInjection;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddLucidServiceDiscovery(
        this IHttpClientBuilder builder,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        builder.ConfigureHttpClient(client =>
        {
            client.BaseAddress ??= new Uri($"http://{serviceName}");
        });
        builder.AddHttpMessageHandler(serviceProvider => new ServiceDiscoveryHttpMessageHandler(
            serviceName,
            serviceProvider.GetRequiredService<IServiceEndpointResolver>(),
            serviceProvider.GetRequiredService<IServiceEndpointSelector>()));

        return builder;
    }
}
