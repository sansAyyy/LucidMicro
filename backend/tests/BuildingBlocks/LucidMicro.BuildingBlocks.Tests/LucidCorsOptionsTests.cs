using LucidMicro.BuildingBlocks.Cors.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Cors.AspNetCore.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class LucidCorsOptionsTests
{
    [Fact]
    public void AddLucidCors_AllowsEmptyOrigins_WhenDisabled()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:Cors:Enabled"] = "false"
        });

        services.AddLucidCors(configuration.GetRequiredSection(LucidCorsOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public void AddLucidCors_Throws_WhenCredentialsUseWildcardOrigin()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:Cors:Enabled"] = "true",
            ["Lucid:Cors:AllowedOrigins:0"] = "*",
            ["Lucid:Cors:AllowedMethods:0"] = "GET",
            ["Lucid:Cors:AllowedHeaders:0"] = "Authorization",
            ["Lucid:Cors:AllowCredentials"] = "true"
        });

        Assert.Throws<ArgumentException>(
            () => services.AddLucidCors(configuration.GetRequiredSection(LucidCorsOptions.ConfigurationSectionName)));
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
