using LucidMicro.BuildingBlocks.Caching.Abstractions.Contracts;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Options;
using LucidMicro.Services.Identity.Infrastructure.DependencyInjection;
using LucidMicro.Services.Identity.Infrastructure.ExternalServices.SmsLogin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.Services.Identity.Infrastructure.Tests;

public sealed class RedisSmsLoginCodeStoreTests
{
    [Fact]
    public void Constructor_Throws_WhenOptionsAreInvalid()
    {
        var cache = new TestCacheService();
        var options = new SmsLoginOptions
        {
            CodeTtlSeconds = 0
        };

        Assert.Throws<InvalidOperationException>(() => new RedisSmsLoginCodeStore(cache, options));
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersSmsLoginStoreAndOptions()
    {
        var services = new ServiceCollection();

        services.AddIdentityInfrastructure(CreateConfiguration());

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<SmsLoginOptions>();

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(ISmsLoginCodeStore)
                       && service.ImplementationType == typeof(RedisSmsLoginCodeStore));
        Assert.Equal(120, options.CodeTtlSeconds);
        Assert.Equal(30, options.SendIntervalSeconds);
        Assert.Equal(180, options.AttemptTtlSeconds);
        Assert.Equal(3, options.MaxAttempts);
    }

    [Fact]
    public async Task CanSendAsync_ReturnsTrue_WhenRateKeyDoesNotExist()
    {
        var cache = new TestCacheService();
        var store = CreateStore(cache);

        var canSend = await store.CanSendAsync("13800138000");

        Assert.True(canSend);
    }

    [Fact]
    public async Task SaveCodeAsync_SavesCodeAndRateLimit()
    {
        var cache = new TestCacheService();
        var store = CreateStore(cache);

        await store.SaveCodeAsync(" 13800138000 ", " 123456 ");

        Assert.Equal("123456", await store.GetCodeAsync("13800138000"));
        Assert.False(await store.CanSendAsync("13800138000"));
        Assert.Equal(TimeSpan.FromSeconds(300), cache.Ttls["identity:sms-login:code:13800138000"]);
        Assert.Equal(TimeSpan.FromSeconds(60), cache.Ttls["identity:sms-login:rate:13800138000"]);
    }

    [Fact]
    public async Task RemoveCodeAsync_RemovesCodeAndAttempts()
    {
        var cache = new TestCacheService();
        var store = CreateStore(cache);

        await store.SaveCodeAsync("13800138000", "123456");
        await store.IncrementAttemptAsync("13800138000");
        await store.RemoveCodeAsync("13800138000");

        Assert.Null(await store.GetCodeAsync("13800138000"));
        Assert.False(cache.Values.ContainsKey("identity:sms-login:attempt:13800138000"));
    }

    [Fact]
    public async Task IncrementAttemptAsync_IncrementsAttemptCount()
    {
        var cache = new TestCacheService();
        var store = CreateStore(cache);

        var first = await store.IncrementAttemptAsync("13800138000");
        var second = await store.IncrementAttemptAsync("13800138000");

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(TimeSpan.FromSeconds(300), cache.Ttls["identity:sms-login:attempt:13800138000"]);
    }

    private static RedisSmsLoginCodeStore CreateStore(TestCacheService cache)
    {
        return new RedisSmsLoginCodeStore(cache, new SmsLoginOptions());
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Lucid:Caching:Redis:ConnectionString"] = "localhost:6379",
                ["Lucid:Identity:SmsLogin:CodeTtlSeconds"] = "120",
                ["Lucid:Identity:SmsLogin:SendIntervalSeconds"] = "30",
                ["Lucid:Identity:SmsLogin:AttemptTtlSeconds"] = "180",
                ["Lucid:Identity:SmsLogin:MaxAttempts"] = "3",
                ["Lucid:Resilience:Http:Enabled"] = "false",
                ["Lucid:ServiceDiscovery:Consul:Address"] = "http://consul:8500",
                ["Lucid:ServiceDiscovery:Consul:RequestTimeoutSeconds"] = "3",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceName"] = "identity",
                ["Lucid:ServiceDiscovery:Consul:Registration:ServiceId"] = "identity-test",
                ["Lucid:ServiceDiscovery:Consul:Registration:Address"] = "localhost",
                ["Lucid:ServiceDiscovery:Consul:Registration:Port"] = "49753",
                ["Lucid:EventBus:RabbitMQ:ConnectionString"] = "amqp://guest:guest@localhost:5672/",
                ["Lucid:EventBus:RabbitMQ:ExchangeName"] = "lucid.events",
                ["Authentication:Jwt:Issuer"] = "LucidMicro.Identity",
                ["Authentication:Jwt:Audience"] = "LucidMicro.Admin",
                ["Authentication:Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes"
            })
            .Build();
    }

    private sealed class TestCacheService : ICacheService
    {
        public Dictionary<string, object> Values { get; } = [];

        public Dictionary<string, TimeSpan?> Ttls { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (!Values.TryGetValue(key, out var value))
            {
                return Task.FromResult(default(T));
            }

            return Task.FromResult((T?)value);
        }

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(value);

            Values[key] = value;
            Ttls[key] = ttl;

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Values.Remove(key);
            Ttls.Remove(key);

            return Task.CompletedTask;
        }
    }
}
