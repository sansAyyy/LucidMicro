using LucidMicro.BuildingBlocks.DistributedLock.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.DistributedLock.Redis.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.DistributedLock.Redis.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidRedisDistributedLock(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();

        return services;
    }
}
