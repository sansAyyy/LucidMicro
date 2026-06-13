using System.Text.Json;
using LucidMicro.BuildingBlocks.Caching.Abstractions.Contracts;
using StackExchange.Redis;

namespace LucidMicro.BuildingBlocks.Caching.Redis.Services;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _database;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfKeyIsInvalid(key);

        var value = await _database.StringGetAsync(key).WaitAsync(cancellationToken);
        if (value.IsNull)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>((byte[]?)value!, JsonOptions);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfKeyIsInvalid(key);
        ArgumentNullException.ThrowIfNull(value);

        if (ttl.HasValue && ttl.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "ttl must be greater than zero.");
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var expiration = ttl.HasValue
            ? new Expiration(ttl.Value)
            : Expiration.Default;

        return _database.StringSetAsync(key, bytes, expiration).WaitAsync(cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfKeyIsInvalid(key);

        return _database.KeyDeleteAsync(key).WaitAsync(cancellationToken);
    }

    private static void ThrowIfKeyIsInvalid(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }
}
