using LucidMicro.BuildingBlocks.DistributedLock.Abstractions.Contracts;
using StackExchange.Redis;

namespace LucidMicro.BuildingBlocks.DistributedLock.Redis.Services;

internal sealed class RedisDistributedLockHandle : IDistributedLockHandle
{
    private const string ReleaseScript = """
        if redis.call("get", KEYS[1]) == ARGV[1] then
            return redis.call("del", KEYS[1])
        else
            return 0
        end
        """;

    private readonly IDatabase _database;
    private readonly string _token;
    private bool _disposed;

    public RedisDistributedLockHandle(IDatabase database, string key, string token)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        _database = database;
        Key = key;
        _token = token;
    }

    public string Key { get; }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _database.ScriptEvaluateAsync(
            ReleaseScript,
            [Key],
            [_token]);
    }
}
