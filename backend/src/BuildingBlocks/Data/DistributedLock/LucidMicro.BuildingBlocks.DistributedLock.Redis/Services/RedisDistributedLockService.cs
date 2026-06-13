using LucidMicro.BuildingBlocks.DistributedLock.Abstractions.Contracts;
using StackExchange.Redis;

namespace LucidMicro.BuildingBlocks.DistributedLock.Redis.Services;

public sealed class RedisDistributedLockService : IDistributedLockService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly IDatabase _database;

    public RedisDistributedLockService(IConnectionMultiplexer connectionMultiplexer)
    {
        ArgumentNullException.ThrowIfNull(connectionMultiplexer);

        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<IDistributedLockHandle?> TryAcquireAsync(
        string key,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        ThrowIfKeyIsInvalid(key);
        ThrowIfExpiryIsInvalid(expiry);

        var token = Guid.NewGuid().ToString("N");
        var acquired = await _database
            .StringSetAsync(
                key,
                token,
                expiry,
                when: When.NotExists)
            .WaitAsync(cancellationToken);

        return acquired
            ? new RedisDistributedLockHandle(_database, key, token)
            : null;
    }

    public async Task<IDistributedLockHandle?> WaitAcquireAsync(
        string key,
        TimeSpan expiry,
        TimeSpan waitTimeout,
        CancellationToken cancellationToken = default)
    {
        ThrowIfWaitTimeoutIsInvalid(waitTimeout);

        var deadline = DateTimeOffset.UtcNow.Add(waitTimeout);

        while (true)
        {
            var handle = await TryAcquireAsync(key, expiry, cancellationToken);
            if (handle is not null)
            {
                return handle;
            }

            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return null;
            }

            var delay = remaining < RetryDelay ? remaining : RetryDelay;
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static void ThrowIfKeyIsInvalid(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }

    private static void ThrowIfExpiryIsInvalid(TimeSpan expiry)
    {
        if (expiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), "expiry must be greater than zero.");
        }
    }

    private static void ThrowIfWaitTimeoutIsInvalid(TimeSpan waitTimeout)
    {
        if (waitTimeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(waitTimeout), "waitTimeout cannot be negative.");
        }
    }
}
