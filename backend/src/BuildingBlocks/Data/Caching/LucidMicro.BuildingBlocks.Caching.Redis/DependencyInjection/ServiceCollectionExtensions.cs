using LucidMicro.BuildingBlocks.Caching.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Caching.Redis.Options;
using LucidMicro.BuildingBlocks.Caching.Redis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LucidMicro.BuildingBlocks.Caching.Redis.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidRedisCaching(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = LucidRedisCacheOptions.FromConfiguration(configurationSection);
        options.Validate();

        services
            .AddOptions<LucidRedisCacheOptions>()
            .Bind(configurationSection)
            .Validate(ValidateOptions, "Lucid Redis cache options are invalid.")
            .ValidateOnStart();

        services.AddSingleton(options);
        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(options.ConnectionString));
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    private static bool ValidateOptions(LucidRedisCacheOptions options)
    {
        try
        {
            options.Validate();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
