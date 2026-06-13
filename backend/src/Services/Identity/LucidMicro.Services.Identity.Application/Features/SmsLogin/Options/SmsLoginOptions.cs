namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Options;

public sealed class SmsLoginOptions
{
    public const string ConfigurationSectionName = "Lucid:Identity:SmsLogin";

    public int CodeTtlSeconds { get; set; } = 300;

    public int SendIntervalSeconds { get; set; } = 60;

    public int AttemptTtlSeconds { get; set; } = 300;

    public int MaxAttempts { get; set; } = 5;

    public void Validate()
    {
        if (CodeTtlSeconds <= 0)
        {
            throw new InvalidOperationException("SMS login code TTL seconds must be greater than zero.");
        }

        if (SendIntervalSeconds <= 0)
        {
            throw new InvalidOperationException("SMS login send interval seconds must be greater than zero.");
        }

        if (AttemptTtlSeconds <= 0)
        {
            throw new InvalidOperationException("SMS login attempt TTL seconds must be greater than zero.");
        }

        if (MaxAttempts <= 0)
        {
            throw new InvalidOperationException("SMS login max attempts must be greater than zero.");
        }
    }
}
