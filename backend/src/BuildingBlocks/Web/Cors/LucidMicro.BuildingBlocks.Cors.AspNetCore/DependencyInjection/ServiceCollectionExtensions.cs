using LucidMicro.BuildingBlocks.Cors.AspNetCore.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.Cors.AspNetCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidCors(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = configurationSection.Get<LucidCorsOptions>() ?? new LucidCorsOptions();
        if (!ValidateOptions(options))
        {
            throw new ArgumentException("Lucid CORS options are invalid.", nameof(configurationSection));
        }

        services
            .AddOptions<LucidCorsOptions>()
            .Bind(configurationSection)
            .Validate(ValidateOptions, "Lucid CORS options are invalid.")
            .ValidateOnStart();

        services.AddCors(corsOptions =>
        {
            corsOptions.AddPolicy(
                LucidCorsOptions.PolicyName,
                policy => ConfigurePolicy(policy, options));
        });

        return services;
    }

    private static void ConfigurePolicy(CorsPolicyBuilder policy, LucidCorsOptions options)
    {
        ConfigureOrigins(policy, options);
        ConfigureMethods(policy, options);
        ConfigureHeaders(policy, options);

        if (options.AllowCredentials)
        {
            policy.AllowCredentials();
        }
    }

    private static void ConfigureOrigins(CorsPolicyBuilder policy, LucidCorsOptions options)
    {
        if (ContainsWildcard(options.AllowedOrigins))
        {
            policy.AllowAnyOrigin();
            return;
        }

        policy.WithOrigins(options.AllowedOrigins);
    }

    private static void ConfigureMethods(CorsPolicyBuilder policy, LucidCorsOptions options)
    {
        if (ContainsWildcard(options.AllowedMethods))
        {
            policy.AllowAnyMethod();
            return;
        }

        policy.WithMethods(options.AllowedMethods);
    }

    private static void ConfigureHeaders(CorsPolicyBuilder policy, LucidCorsOptions options)
    {
        if (ContainsWildcard(options.AllowedHeaders))
        {
            policy.AllowAnyHeader();
            return;
        }

        policy.WithHeaders(options.AllowedHeaders);
    }

    private static bool ValidateOptions(LucidCorsOptions options)
    {
        if (!options.Enabled)
        {
            return true;
        }

        if (options.AllowedOrigins.Length == 0
            || options.AllowedMethods.Length == 0
            || options.AllowedHeaders.Length == 0)
        {
            return false;
        }

        return !options.AllowCredentials || !ContainsWildcard(options.AllowedOrigins);
    }

    private static bool ContainsWildcard(IEnumerable<string> values)
    {
        return values.Any(value => value == "*");
    }
}
