using LucidMicro.BuildingBlocks.Resilience.Http.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.Resilience.Http.DependencyInjection;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddLucidStandardHttpResilienceHandler(
        this IHttpClientBuilder builder,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = configurationSection.Get<LucidHttpResilienceOptions>() ?? new LucidHttpResilienceOptions();

        return builder.AddLucidStandardHttpResilienceHandler(options);
    }

    public static IHttpClientBuilder AddLucidStandardHttpResilienceHandler(
        this IHttpClientBuilder builder,
        LucidHttpResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        if (!options.Enabled)
        {
            return builder;
        }

        builder.AddStandardResilienceHandler(standardOptions =>
        {
            standardOptions.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TotalRequestTimeoutSeconds);
            standardOptions.AttemptTimeout.Timeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds);

            standardOptions.Retry.MaxRetryAttempts = options.Retry.MaxRetryAttempts;
            standardOptions.Retry.Delay = TimeSpan.FromMilliseconds(options.Retry.DelayMilliseconds);

            standardOptions.CircuitBreaker.FailureRatio = options.CircuitBreaker.FailureRatio;
            standardOptions.CircuitBreaker.MinimumThroughput = options.CircuitBreaker.MinimumThroughput;
            standardOptions.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds);
            standardOptions.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds);
        });

        return builder;
    }
}
