using System.Reflection;
using LucidMicro.BuildingBlocks.DistributedLock.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.DistributedLock.Redis.DependencyInjection;
using LucidMicro.BuildingBlocks.DistributedLock.Redis.Services;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class RedisDistributedLockServiceTests
{
    [Fact]
    public void AddLucidRedisDistributedLock_RegistersDistributedLockService()
    {
        var services = new ServiceCollection();

        services.AddLucidRedisDistributedLock();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IDistributedLockService));

        Assert.Equal(typeof(RedisDistributedLockService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void Constructor_Throws_WhenConnectionMultiplexerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedLockService(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task TryAcquireAsync_Throws_WhenKeyIsInvalid(string? key)
    {
        var service = CreateService();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.TryAcquireAsync(key!, TimeSpan.FromSeconds(30)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task TryAcquireAsync_Throws_WhenExpiryIsNotPositive(int expirySeconds)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.TryAcquireAsync("lock-key", TimeSpan.FromSeconds(expirySeconds)));
    }

    [Fact]
    public async Task TryAcquireAsync_ReturnsNull_WhenRedisDoesNotAcquireLock()
    {
        var database = CreateDatabase(stringSetResult: false);
        var service = CreateService(database);

        var handle = await service.TryAcquireAsync("lock-key", TimeSpan.FromSeconds(30));

        Assert.Null(handle);
        Assert.Equal(1, database.StringSetCallCount);
    }

    [Fact]
    public async Task TryAcquireAsync_ReturnsHandle_WhenRedisAcquiresLock()
    {
        var database = CreateDatabase(stringSetResult: true);
        var service = CreateService(database);

        var handle = await service.TryAcquireAsync("lock-key", TimeSpan.FromSeconds(30));

        Assert.NotNull(handle);
        Assert.Equal("lock-key", handle.Key);
        Assert.Equal(1, database.StringSetCallCount);
        Assert.Equal(When.NotExists, database.LastStringSetWhen);
    }

    [Fact]
    public async Task DisposeAsync_ReleasesLockOnlyOnce()
    {
        var database = CreateDatabase(stringSetResult: true);
        var service = CreateService(database);
        var handle = await service.TryAcquireAsync("lock-key", TimeSpan.FromSeconds(30));

        await handle!.DisposeAsync();
        await handle.DisposeAsync();

        Assert.Equal(1, database.ScriptEvaluateCallCount);
        Assert.Equal("lock-key", Assert.Single(database.LastScriptKeys ?? []));
        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(database.LastScriptValues ?? [])));
    }

    [Fact]
    public async Task WaitAcquireAsync_ReturnsNull_WhenTimeoutIsZeroAndLockIsUnavailable()
    {
        var database = CreateDatabase(stringSetResult: false);
        var service = CreateService(database);

        var handle = await service.WaitAcquireAsync(
            "lock-key",
            TimeSpan.FromSeconds(30),
            TimeSpan.Zero);

        Assert.Null(handle);
        Assert.Equal(1, database.StringSetCallCount);
    }

    [Fact]
    public async Task WaitAcquireAsync_Throws_WhenWaitTimeoutIsNegative()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.WaitAcquireAsync(
                "lock-key",
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMilliseconds(-1)));
    }

    private static RedisDistributedLockService CreateService()
    {
        return CreateService(CreateDatabase(stringSetResult: true));
    }

    private static RedisDistributedLockService CreateService(RedisDatabaseProxy database)
    {
        var connectionMultiplexer = DispatchProxy.Create<IConnectionMultiplexer, RedisConnectionMultiplexerProxy>();
        ((RedisConnectionMultiplexerProxy)(object)connectionMultiplexer).Database = (IDatabase)(object)database;

        return new RedisDistributedLockService(connectionMultiplexer);
    }

    private static RedisDatabaseProxy CreateDatabase(bool stringSetResult)
    {
        var database = DispatchProxy.Create<IDatabase, RedisDatabaseProxy>();
        var proxy = (RedisDatabaseProxy)(object)database;
        proxy.StringSetResult = stringSetResult;

        return proxy;
    }

    private class RedisConnectionMultiplexerProxy : DispatchProxy
    {
        public IDatabase? Database { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IConnectionMultiplexer.GetDatabase))
            {
                return Database;
            }

            return GetDefaultValue(targetMethod?.ReturnType);
        }
    }

    private class RedisDatabaseProxy : DispatchProxy
    {
        public bool StringSetResult { get; set; }

        public int StringSetCallCount { get; private set; }

        public When? LastStringSetWhen { get; private set; }

        public int ScriptEvaluateCallCount { get; private set; }

        public RedisKey[]? LastScriptKeys { get; private set; }

        public RedisValue[]? LastScriptValues { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IDatabase.StringSetAsync)
                && targetMethod.ReturnType == typeof(Task<bool>))
            {
                StringSetCallCount++;
                LastStringSetWhen = args?.OfType<When>().SingleOrDefault();

                return Task.FromResult(StringSetResult);
            }

            if (targetMethod?.Name == nameof(IDatabase.ScriptEvaluateAsync))
            {
                ScriptEvaluateCallCount++;
                LastScriptKeys = args?.OfType<RedisKey[]>().SingleOrDefault();
                LastScriptValues = args?.OfType<RedisValue[]>().SingleOrDefault();

                return Task.FromResult<RedisResult>(null!);
            }

            return GetDefaultValue(targetMethod?.ReturnType);
        }
    }

    private static object? GetDefaultValue(Type? type)
    {
        if (type is null || type == typeof(void))
        {
            return null;
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
