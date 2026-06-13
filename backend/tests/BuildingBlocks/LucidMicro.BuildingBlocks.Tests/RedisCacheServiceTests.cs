using System.Reflection;
using LucidMicro.BuildingBlocks.Caching.Redis.Services;
using StackExchange.Redis;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class RedisCacheServiceTests
{
    [Fact]
    public void Constructor_Throws_WhenConnectionMultiplexerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RedisCacheService(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetAsync_Throws_WhenKeyIsInvalid(string? key)
    {
        var service = CreateService();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.GetAsync<string>(key!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SetAsync_Throws_WhenKeyIsInvalid(string? key)
    {
        var service = CreateService();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.SetAsync(key!, "value"));
    }

    [Fact]
    public async Task SetAsync_Throws_WhenValueIsNull()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SetAsync<string>("cache-key", null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SetAsync_Throws_WhenTtlIsNotPositive(int ttlSeconds)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.SetAsync("cache-key", "value", TimeSpan.FromSeconds(ttlSeconds)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RemoveAsync_Throws_WhenKeyIsInvalid(string? key)
    {
        var service = CreateService();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.RemoveAsync(key!));
    }

    private static RedisCacheService CreateService()
    {
        var database = DispatchProxy.Create<IDatabase, RedisDatabaseProxy>();
        var connectionMultiplexer = DispatchProxy.Create<IConnectionMultiplexer, RedisConnectionMultiplexerProxy>();
        ((RedisConnectionMultiplexerProxy)(object)connectionMultiplexer).Database = database;

        return new RedisCacheService(connectionMultiplexer);
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
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
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
