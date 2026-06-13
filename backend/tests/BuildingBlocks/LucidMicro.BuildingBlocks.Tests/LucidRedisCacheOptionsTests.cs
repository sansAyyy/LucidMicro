using LucidMicro.BuildingBlocks.Caching.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Caching.Redis.DependencyInjection;
using LucidMicro.BuildingBlocks.Caching.Redis.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class LucidRedisCacheOptionsTests
{
    [Fact]
    public void FromConfiguration_BindsConnectionString()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionString"] = "localhost:6379"
        };

        var options = LucidRedisCacheOptions.FromConfiguration(configuration);

        Assert.Equal("localhost:6379", options.ConnectionString);
    }

    [Fact]
    public void Validate_Throws_WhenConnectionStringIsMissing()
    {
        var options = new LucidRedisCacheOptions();

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void AddLucidRedisCaching_RegistersOptionsAndCacheService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager
        {
            ["Lucid:Caching:Redis:ConnectionString"] = "localhost:6379"
        };

        services.AddLucidRedisCaching(
            configuration.GetRequiredSection(LucidRedisCacheOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<LucidRedisCacheOptions>>().Value;

        Assert.Equal("localhost:6379", options.ConnectionString);
        Assert.Contains(
            services,
            service => service.ServiceType == typeof(ICacheService));
    }
}
