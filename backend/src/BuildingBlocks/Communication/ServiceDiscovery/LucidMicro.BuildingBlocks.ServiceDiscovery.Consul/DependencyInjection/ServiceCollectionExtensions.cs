using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Abstractions.Services;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidConsulServiceDiscovery(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = configurationSection.Get<LucidConsulServiceDiscoveryOptions>()
            ?? new LucidConsulServiceDiscoveryOptions();
        options.Validate();

        services
            .AddOptions<LucidConsulServiceDiscoveryOptions>()
            .Bind(configurationSection)
            .Validate(ValidateOptions, "Lucid Consul service discovery options are invalid.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(options);
        services
            .AddHttpClient(ConsulServiceEndpointResolver.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(options.Address);
                client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
            });
        services.AddSingleton<IServiceEndpointResolver>(serviceProvider =>
            new ConsulServiceEndpointResolver(
                serviceProvider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(ConsulServiceEndpointResolver.HttpClientName),
                options,
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.TryAddSingleton<IServiceEndpointSelector, RoundRobinServiceEndpointSelector>();

        return services;
    }

    public static IServiceCollection AddLucidConsulServiceRegistration(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = configurationSection.Get<LucidConsulServiceRegistrationOptions>()
            ?? new LucidConsulServiceRegistrationOptions();
        options.Validate();

        services
            .AddOptions<LucidConsulServiceRegistrationOptions>()
            .Bind(configurationSection)
            .Validate(ValidateRegistrationOptions, "Lucid Consul service registration options are invalid.")
            .ValidateOnStart();

        services.AddSingleton(options);
        services
            .AddHttpClient(ConsulServiceRegistrationHostedService.HttpClientName, (serviceProvider, client) =>
            {
                var discoveryOptions = serviceProvider.GetRequiredService<LucidConsulServiceDiscoveryOptions>();

                client.BaseAddress = new Uri(discoveryOptions.Address);
                client.Timeout = TimeSpan.FromSeconds(discoveryOptions.RequestTimeoutSeconds);
            });
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, ConsulServiceRegistrationHostedService>());

        return services;
    }

    private static bool ValidateOptions(LucidConsulServiceDiscoveryOptions options)
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

    private static bool ValidateRegistrationOptions(LucidConsulServiceRegistrationOptions options)
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
