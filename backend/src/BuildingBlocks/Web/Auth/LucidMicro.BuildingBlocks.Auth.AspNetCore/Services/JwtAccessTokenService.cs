using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.Services;

public sealed class JwtAccessTokenService : IAccessTokenService, IRefreshTokenService, IRefreshTokenValidator
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    private readonly JwtAccessTokenOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtAccessTokenService(
        IOptions<JwtAccessTokenOptions> options,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public AccessToken GenerateAccessToken(AccessTokenClaims claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentException.ThrowIfNullOrWhiteSpace(claims.Subject);

        var token = GenerateToken(claims, _options.Audience, _options.ExpirationMinutes);

        return new AccessToken(token.Token, token.ExpiresAt);
    }

    public RefreshToken GenerateRefreshToken(AccessTokenClaims claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentException.ThrowIfNullOrWhiteSpace(claims.Subject);

        var token = GenerateToken(claims, _options.RefreshAudience, _options.RefreshExpirationMinutes);

        return new RefreshToken(token.Token, token.ExpiresAt);
    }

    public AccessTokenClaims? ValidateRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(
                refreshToken,
                CreateRefreshTokenValidationParameters(),
                out _);

            return CreateAccessTokenClaims(principal);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private GeneratedToken GenerateToken(
        AccessTokenClaims claims,
        string audience,
        int expirationMinutes)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(expirationMinutes);
        var tokenClaims = CreateClaims(claims);
        var signingCredentials = CreateSigningCredentials(_options.SigningKey);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: audience,
            claims: tokenClaims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        return new GeneratedToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    private static List<Claim> CreateClaims(AccessTokenClaims claims)
    {
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, claims.Subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (!string.IsNullOrWhiteSpace(claims.Name))
        {
            tokenClaims.Add(new Claim(ClaimTypes.Name, claims.Name));
        }

        if (claims.AdditionalClaims is null)
        {
            return tokenClaims;
        }

        foreach (var (type, value) in claims.AdditionalClaims)
        {
            if (string.IsNullOrWhiteSpace(type)
                || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            tokenClaims.Add(new Claim(type, value));
        }

        return tokenClaims;
    }

    private AccessTokenClaims? CreateAccessTokenClaims(ClaimsPrincipal principal)
    {
        var subject = FindClaimValue(
            principal,
            ClaimTypes.NameIdentifier,
            JwtRegisteredClaimNames.Sub,
            "sub");

        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var name = FindClaimValue(
            principal,
            ClaimTypes.Name,
            JwtRegisteredClaimNames.Name,
            "name");
        var email = FindClaimValue(
            principal,
            ClaimTypes.Email,
            JwtRegisteredClaimNames.Email,
            "email");
        var additionalClaims = string.IsNullOrWhiteSpace(email)
            ? null
            : new Dictionary<string, string>
            {
                ["email"] = email
            };

        return new AccessTokenClaims(subject, name, additionalClaims);
    }

    private TokenValidationParameters CreateRefreshTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.RefreshAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            ClockSkew = ClockSkew,
            LifetimeValidator = ValidateLifetime
        };
    }

    private bool ValidateLifetime(
        DateTime? notBefore,
        DateTime? expires,
        SecurityToken securityToken,
        TokenValidationParameters validationParameters)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return expires is not null
            && expires.Value > now.Subtract(ClockSkew)
            && (notBefore is null || notBefore.Value <= now.Add(ClockSkew));
    }

    private static string? FindClaimValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        return claimTypes
            .Select(claimType => principal.FindFirst(claimType)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static SigningCredentials CreateSigningCredentials(string signingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signingKey);

        return new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
    }

    private sealed record GeneratedToken(
        string Token,
        DateTimeOffset ExpiresAt);
}
