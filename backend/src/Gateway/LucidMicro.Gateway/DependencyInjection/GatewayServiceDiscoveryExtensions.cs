using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.DependencyInjection;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using LucidMicro.Gateway.Options;
using LucidMicro.Gateway.ServiceDiscovery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace LucidMicro.Gateway.DependencyInjection;

public static class GatewayServiceDiscoveryExtensions
{
    public static IReverseProxyBuilder AddLucidGatewayReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration
            .GetRequiredSection(LucidGatewayServiceDiscoveryOptions.ConfigurationSectionName)
            .Get<LucidGatewayServiceDiscoveryOptions>()
            ?? new LucidGatewayServiceDiscoveryOptions();
        options.Validate();

        services
            .AddOptions<LucidGatewayServiceDiscoveryOptions>()
            .Bind(configuration.GetRequiredSection(LucidGatewayServiceDiscoveryOptions.ConfigurationSectionName))
            .Validate(ValidateOptions, "Lucid Gateway service discovery options are invalid.")
            .ValidateOnStart();
        services.AddSingleton(options);

        var reverseProxyBuilder = services.AddReverseProxy();
        if (!options.Enabled)
        {
            return reverseProxyBuilder.LoadFromConfig(configuration.GetRequiredSection("ReverseProxy"));
        }

        services.AddLucidConsulServiceDiscovery(
            configuration.GetRequiredSection(LucidConsulServiceDiscoveryOptions.ConfigurationSectionName));
        services.AddSingleton<GatewayConsulProxyConfigProvider>();
        services.AddSingleton<IProxyConfigProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<GatewayConsulProxyConfigProvider>());
        services.AddHostedService<GatewayConsulProxyConfigRefreshHostedService>();

        return reverseProxyBuilder;
    }

    private static bool ValidateOptions(LucidGatewayServiceDiscoveryOptions options)
    {
        try
        {
            options.Validate();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
