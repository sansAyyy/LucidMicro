using System.Reflection;
using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.HealthChecks.Redis;
using LucidMicro.BuildingBlocks.HealthChecks.Redis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class RedisHealthChecksBuilderExtensionsTests
{
    [Fact]
    public void AddLucidRedisCheck_RegistersReadyCacheCheck()
    {
        var services = new ServiceCollection();
        var connectionMultiplexer = DispatchProxy.Create<IConnectionMultiplexer, RedisConnectionMultiplexerProxy>();

        services.AddSingleton(connectionMultiplexer);

        services.AddHealthChecks()
            .AddLucidRedisCheck();

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = Assert.Single(options.Registrations);

        Assert.Equal(LucidHealthCheckTags.Redis, registration.Name);
        Assert.Equal(typeof(RedisHealthCheck), registration.Factory(serviceProvider).GetType());
        Assert.Contains(LucidHealthCheckTags.Ready, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.Cache, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.Redis, registration.Tags);
    }

    private class RedisConnectionMultiplexerProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return null;
        }
    }
}
