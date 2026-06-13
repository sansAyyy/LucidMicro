using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class JwtAccessTokenOptionsTests
{
    [Fact]
    public void AddLucidJwtAuthentication_RegistersOptionsAndTokenServices()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Issuer"] = "LucidMicro.Identity",
            ["Authentication:Jwt:Audience"] = "LucidMicro.Admin",
            ["Authentication:Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes",
            ["Authentication:Jwt:ExpirationMinutes"] = "30",
            ["Authentication:Jwt:RefreshExpirationMinutes"] = "10080"
        });
        var services = new ServiceCollection();

        services.AddLucidJwtAuthentication(
            configuration.GetRequiredSection(JwtAccessTokenOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<JwtAccessTokenOptions>>().Value;

        Assert.Equal("LucidMicro.Identity", options.Issuer);
        Assert.Equal("LucidMicro.Admin", options.Audience);
        Assert.Equal("LucidMicro.Admin.Refresh", options.RefreshAudience);
        Assert.Equal(30, options.ExpirationMinutes);
        Assert.NotNull(serviceProvider.GetRequiredService<IAccessTokenService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IRefreshTokenService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IRefreshTokenValidator>());
    }

    [Fact]
    public void AddLucidJwtAuthentication_Throws_WhenSigningKeyIsMissing()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Issuer"] = "LucidMicro.Identity",
            ["Authentication:Jwt:Audience"] = "LucidMicro.Admin"
        });
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddLucidJwtAuthentication(
            configuration.GetRequiredSection(JwtAccessTokenOptions.ConfigurationSectionName)));
    }

    private static IConfigurationRoot CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
