using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Options;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Services;
using LucidMicro.Tests.Shared.Time;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class JwtAccessTokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_CreatesJwtWithConfiguredClaimsAndExpiration()
    {
        var now = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);
        var service = new JwtAccessTokenService(
            Options.Create(new JwtAccessTokenOptions
            {
                Issuer = "LucidMicro.Identity",
                Audience = "LucidMicro.Admin",
                RefreshAudience = "LucidMicro.Admin.Refresh",
                SigningKey = "test-signing-key-with-at-least-32-bytes",
                ExpirationMinutes = 30
            }),
            new TestTimeProvider(now));

        var accessToken = service.GenerateAccessToken(new AccessTokenClaims(
            "admin-id",
            "admin",
            new Dictionary<string, string>
            {
                ["email"] = "admin@example.com"
            }));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);

        Assert.Equal(now.AddMinutes(30), accessToken.ExpiresAt);
        Assert.Equal("LucidMicro.Identity", jwt.Issuer);
        Assert.Contains("LucidMicro.Admin", jwt.Audiences);
        Assert.Equal("admin-id", jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("admin", jwt.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.Equal("admin@example.com", jwt.Claims.Single(claim => claim.Type == "email").Value);
    }

    [Fact]
    public void GenerateRefreshToken_CreatesJwtWithConfiguredRefreshAudienceAndExpiration()
    {
        var now = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);
        var service = new JwtAccessTokenService(
            Options.Create(new JwtAccessTokenOptions
            {
                Issuer = "LucidMicro.Identity",
                Audience = "LucidMicro.Admin",
                RefreshAudience = "LucidMicro.Admin.Refresh",
                SigningKey = "test-signing-key-with-at-least-32-bytes",
                RefreshExpirationMinutes = 10080
            }),
            new TestTimeProvider(now));

        var refreshToken = service.GenerateRefreshToken(new AccessTokenClaims(
            "admin-id",
            "admin",
            new Dictionary<string, string>
            {
                ["email"] = "admin@example.com"
            }));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(refreshToken.Token);

        Assert.Equal(now.AddMinutes(10080), refreshToken.ExpiresAt);
        Assert.Equal("LucidMicro.Identity", jwt.Issuer);
        Assert.Contains("LucidMicro.Admin.Refresh", jwt.Audiences);
        Assert.DoesNotContain("LucidMicro.Admin", jwt.Audiences);
        Assert.Equal("admin-id", jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("admin", jwt.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.Equal("admin@example.com", jwt.Claims.Single(claim => claim.Type == "email").Value);
    }

    [Fact]
    public void ValidateRefreshToken_ReturnsClaims_WhenTokenIsValid()
    {
        var now = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);
        var service = CreateJwtAccessTokenService(now);
        var refreshToken = service.GenerateRefreshToken(new AccessTokenClaims(
            "admin-id",
            "admin",
            new Dictionary<string, string>
            {
                ["email"] = "admin@example.com"
            }));

        var claims = service.ValidateRefreshToken(refreshToken.Token);

        Assert.NotNull(claims);
        Assert.Equal("admin-id", claims.Subject);
        Assert.Equal("admin", claims.Name);
        Assert.Equal("admin@example.com", claims.AdditionalClaims?["email"]);
    }

    [Fact]
    public void ValidateRefreshToken_ReturnsNull_WhenTokenUsesAccessTokenAudience()
    {
        var service = CreateJwtAccessTokenService(new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero));
        var accessToken = service.GenerateAccessToken(new AccessTokenClaims("admin-id"));

        var claims = service.ValidateRefreshToken(accessToken.Token);

        Assert.Null(claims);
    }

    [Fact]
    public void ValidateRefreshToken_ReturnsNull_WhenTokenIsExpired()
    {
        var issuedAt = new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero);
        var service = CreateJwtAccessTokenService(issuedAt, refreshExpirationMinutes: 1);
        var refreshToken = service.GenerateRefreshToken(new AccessTokenClaims("admin-id"));
        var validatingService = CreateJwtAccessTokenService(issuedAt.AddMinutes(3), refreshExpirationMinutes: 1);

        var claims = validatingService.ValidateRefreshToken(refreshToken.Token);

        Assert.Null(claims);
    }

    private static JwtAccessTokenService CreateJwtAccessTokenService(
        DateTimeOffset utcNow,
        int refreshExpirationMinutes = 10080)
    {
        return new JwtAccessTokenService(
            Options.Create(new JwtAccessTokenOptions
            {
                Issuer = "LucidMicro.Identity",
                Audience = "LucidMicro.Admin",
                RefreshAudience = "LucidMicro.Admin.Refresh",
                SigningKey = "test-signing-key-with-at-least-32-bytes",
                ExpirationMinutes = 30,
                RefreshExpirationMinutes = refreshExpirationMinutes
            }),
            new TestTimeProvider(utcNow));
    }
}
