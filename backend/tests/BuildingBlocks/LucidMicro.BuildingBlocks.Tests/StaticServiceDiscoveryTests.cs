using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Exceptions;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Services;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Static.DependencyInjection;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Static.Options;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Static.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class StaticServiceDiscoveryTests
{
    [Fact]
    public async Task AddLucidStaticServiceDiscovery_RegistersResolverSelectorAndOptions()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:ServiceDiscovery:Services:notification:0"] = "http://localhost:49853",
            ["Lucid:ServiceDiscovery:Services:notification:1"] = "https://notification.internal"
        });
        var services = new ServiceCollection();

        services.AddLucidStaticServiceDiscovery(
            configuration.GetRequiredSection(LucidStaticServiceDiscoveryOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<LucidStaticServiceDiscoveryOptions>>().Value;
        var resolver = serviceProvider.GetRequiredService<IServiceEndpointResolver>();
        var selector = serviceProvider.GetRequiredService<IServiceEndpointSelector>();

        Assert.True(options.Services.ContainsKey("notification"));

        var endpoints = await resolver.ResolveAsync("notification");

        Assert.Equal(2, endpoints.Count);
        Assert.Equal(new Uri("http://localhost:49853"), selector.Select("notification", endpoints));
    }

    [Fact]
    public async Task StaticServiceEndpointResolver_ReturnsEmptyList_WhenServiceIsUnknown()
    {
        var resolver = new StaticServiceEndpointResolver(new LucidStaticServiceDiscoveryOptions
        {
            Services = new Dictionary<string, string[]>
            {
                ["notification"] = ["http://localhost:49853"]
            }
        });

        var endpoints = await resolver.ResolveAsync("identity");

        Assert.Empty(endpoints);
    }

    [Fact]
    public void RoundRobinServiceEndpointSelector_RotatesEndpoints()
    {
        var selector = new RoundRobinServiceEndpointSelector();
        var endpoints = new[]
        {
            new Uri("http://localhost:49853"),
            new Uri("http://localhost:49854")
        };

        var first = selector.Select("notification", endpoints);
        var second = selector.Select("notification", endpoints);
        var third = selector.Select("notification", endpoints);

        Assert.Equal(endpoints[0], first);
        Assert.Equal(endpoints[1], second);
        Assert.Equal(endpoints[0], third);
    }

    [Fact]
    public void RoundRobinServiceEndpointSelector_Throws_WhenEndpointsAreEmpty()
    {
        var selector = new RoundRobinServiceEndpointSelector();

        Assert.Throws<ServiceEndpointNotFoundException>(() => selector.Select("notification", []));
    }

    [Fact]
    public void AddLucidStaticServiceDiscovery_Throws_WhenEndpointIsInvalid()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:ServiceDiscovery:Services:notification:0"] = "localhost:49853"
        });
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddLucidStaticServiceDiscovery(
            configuration.GetRequiredSection(LucidStaticServiceDiscoveryOptions.ConfigurationSectionName)));
    }

    private static IConfigurationRoot CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
