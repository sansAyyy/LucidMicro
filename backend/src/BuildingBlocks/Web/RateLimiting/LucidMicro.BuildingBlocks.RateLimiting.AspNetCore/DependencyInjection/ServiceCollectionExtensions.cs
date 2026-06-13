using LucidMicro.BuildingBlocks.RateLimiting.AspNetCore.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace LucidMicro.BuildingBlocks.RateLimiting.AspNetCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const string GlobalPartitionKey = "lucid-global";

    public static IServiceCollection AddLucidRateLimiting(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = configurationSection.Get<LucidRateLimitingOptions>() ?? new LucidRateLimitingOptions();
        if (!ValidateOptions(options))
        {
            throw new ArgumentException("Lucid rate limiting options are invalid.", nameof(configurationSection));
        }

        services
            .AddOptions<LucidRateLimitingOptions>()
            .Bind(configurationSection)
            .Validate(ValidateOptions, "Lucid rate limiting options are invalid.")
            .ValidateOnStart();

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = options.RejectionStatusCode;
            rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                _ => RateLimitPartition.GetFixedWindowLimiter(
                    GlobalPartitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        QueueLimit = options.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });

        return services;
    }

    private static bool ValidateOptions(LucidRateLimitingOptions options)
    {
        if (!options.Enabled)
        {
            return true;
        }

        return options.PermitLimit > 0
            && options.WindowSeconds > 0
            && options.QueueLimit >= 0
            && options.RejectionStatusCode is >= 400 and <= 599;
    }
}
