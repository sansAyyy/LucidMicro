using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class TestAccessTokenService : IAccessTokenService, IRefreshTokenService, IRefreshTokenValidator
{
    public AccessTokenClaims? LastClaims { get; private set; }

    public AccessTokenClaims? LastRefreshClaims { get; private set; }

    public AccessTokenClaims? ValidatedRefreshTokenClaims { get; set; }

    public string? LastValidatedRefreshToken { get; private set; }

    public AccessToken GenerateAccessToken(AccessTokenClaims claims)
    {
        LastClaims = claims;

        return new AccessToken(
            $"token:{claims.Subject}",
            new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero));
    }

    public RefreshToken GenerateRefreshToken(AccessTokenClaims claims)
    {
        LastRefreshClaims = claims;

        return new RefreshToken(
            $"refresh-token:{claims.Subject}",
            new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero));
    }

    public AccessTokenClaims? ValidateRefreshToken(string refreshToken)
    {
        LastValidatedRefreshToken = refreshToken;

        return ValidatedRefreshTokenClaims;
    }
}
