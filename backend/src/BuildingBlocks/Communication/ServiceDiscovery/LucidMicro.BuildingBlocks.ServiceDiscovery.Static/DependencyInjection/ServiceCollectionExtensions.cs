using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Services;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Static.Options;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Static.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Static.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidStaticServiceDiscovery(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = configurationSection.Get<LucidStaticServiceDiscoveryOptions>()
            ?? new LucidStaticServiceDiscoveryOptions();
        options.Validate();

        services
            .AddOptions<LucidStaticServiceDiscoveryOptions>()
            .Bind(configurationSection)
            .Validate(ValidateOptions, "Lucid static service discovery options are invalid.")
            .ValidateOnStart();

        services.AddSingleton(options);
        services.AddSingleton<IServiceEndpointResolver, StaticServiceEndpointResolver>();
        services.AddSingleton<IServiceEndpointSelector, RoundRobinServiceEndpointSelector>();

        return services;
    }

    private static bool ValidateOptions(LucidStaticServiceDiscoveryOptions options)
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
