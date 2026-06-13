using LucidMicro.BuildingBlocks.RateLimiting.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.RateLimiting.AspNetCore.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class LucidRateLimitingOptionsTests
{
    [Fact]
    public void AddLucidRateLimiting_AllowsEmptyLimits_WhenDisabled()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:RateLimiting:Enabled"] = "false",
            ["Lucid:RateLimiting:PermitLimit"] = "0",
            ["Lucid:RateLimiting:WindowSeconds"] = "0"
        });

        services.AddLucidRateLimiting(
            configuration.GetRequiredSection(LucidRateLimitingOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<LucidRateLimitingOptions>>().Value;

        Assert.False(options.Enabled);
    }

    [Fact]
    public void AddLucidRateLimiting_Throws_WhenEnabledAndPermitLimitIsInvalid()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:RateLimiting:Enabled"] = "true",
            ["Lucid:RateLimiting:PermitLimit"] = "0",
            ["Lucid:RateLimiting:WindowSeconds"] = "60"
        });

        Assert.Throws<ArgumentException>(
            () => services.AddLucidRateLimiting(
                configuration.GetRequiredSection(LucidRateLimitingOptions.ConfigurationSectionName)));
    }

    [Fact]
    public void AddLucidRateLimiting_Throws_WhenEnabledAndStatusCodeIsInvalid()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:RateLimiting:Enabled"] = "true",
            ["Lucid:RateLimiting:PermitLimit"] = "100",
            ["Lucid:RateLimiting:WindowSeconds"] = "60",
            ["Lucid:RateLimiting:RejectionStatusCode"] = "200"
        });

        Assert.Throws<ArgumentException>(
            () => services.AddLucidRateLimiting(
                configuration.GetRequiredSection(LucidRateLimitingOptions.ConfigurationSectionName)));
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
