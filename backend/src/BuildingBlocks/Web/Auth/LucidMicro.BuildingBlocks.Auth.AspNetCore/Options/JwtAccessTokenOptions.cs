using System.Text;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.Options;

public sealed class JwtAccessTokenOptions
{
    public const string ConfigurationSectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string RefreshAudience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;

    public int RefreshExpirationMinutes { get; set; } = 10080;

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(RefreshAudience);
        ArgumentException.ThrowIfNullOrWhiteSpace(SigningKey);

        if (ExpirationMinutes < 1)
        {
            throw new InvalidOperationException("JWT expiration minutes must be greater than zero.");
        }

        if (RefreshExpirationMinutes < 1)
        {
            throw new InvalidOperationException("JWT refresh expiration minutes must be greater than zero.");
        }

        if (Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        }
    }
}
