using Microsoft.Extensions.Configuration;

namespace LucidMicro.BuildingBlocks.Caching.Redis.Options;

public sealed class LucidRedisCacheOptions
{
    public const string ConfigurationSectionName = "Lucid:Caching:Redis";

    public string ConnectionString { get; set; } = string.Empty;

    public static LucidRedisCacheOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new LucidRedisCacheOptions
        {
            ConnectionString = configuration[nameof(ConnectionString)] ?? string.Empty
        };
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ConnectionString);
    }
}
