using LucidMicro.BuildingBlocks.Resilience.Http.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.BuildingBlocks.Resilience.Http.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidHttpResilience(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = configurationSection.Get<LucidHttpResilienceOptions>() ?? new LucidHttpResilienceOptions();
        options.Validate();

        services
            .AddOptions<LucidHttpResilienceOptions>()
            .Bind(configurationSection)
            .Validate(ValidateOptions, "Lucid HTTP resilience options are invalid.")
            .ValidateOnStart();

        return services;
    }

    private static bool ValidateOptions(LucidHttpResilienceOptions options)
    {
        try
        {
            options.Validate();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
