using LucidMicro.BuildingBlocks.Auth.AspNetCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Options;
using LucidMicro.BuildingBlocks.Caching.Redis.DependencyInjection;
using LucidMicro.BuildingBlocks.Caching.Redis.Options;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.DependencyInjection;
using LucidMicro.BuildingBlocks.EventBus.RabbitMQ.Options;
using LucidMicro.BuildingBlocks.HealthChecks.Consul.DependencyInjection;
using LucidMicro.BuildingBlocks.HealthChecks.Npgsql.DependencyInjection;
using LucidMicro.BuildingBlocks.HealthChecks.RabbitMQ.DependencyInjection;
using LucidMicro.BuildingBlocks.Outbox.Core.DependencyInjection;
using LucidMicro.BuildingBlocks.Outbox.EFCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Persistence.EFCore.DependencyInjection;
using LucidMicro.BuildingBlocks.Resilience.Http.DependencyInjection;
using LucidMicro.BuildingBlocks.Resilience.Http.Options;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.DependencyInjection;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Consul.Options;
using LucidMicro.BuildingBlocks.ServiceDiscovery.Http.DependencyInjection;
using LucidMicro.Services.Identity.Application.ExternalServices.Notifications;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;
using LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Options;
using LucidMicro.Services.Identity.Infrastructure.ExternalServices.Notifications;
using LucidMicro.Services.Identity.Infrastructure.ExternalServices.SmsLogin;
using LucidMicro.Services.Identity.Infrastructure.Persistence;
using LucidMicro.Services.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LucidMicro.Services.Identity.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddPersistence(services, configuration);
        AddAuth(services, configuration);
        AddSharedInfrastructure(services, configuration);
        AddMessaging(services, configuration);
        AddSmsLogin(services, configuration);
        AddNotificationClient(services, configuration);

        return services;
    }

    private static void AddPersistence(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Identity")
            ?? throw new InvalidOperationException("Connection string 'Identity' is not configured.");

        services.AddLucidEfCorePersistence<IdentityDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddLucidEfCoreOutbox<IdentityDbContext>();
        services.AddLucidNpgsqlDbContextHealthCheck<IdentityDbContext>();
        services.AddScoped<IReadOnlyAdminUserPermissionRepository, ReadOnlyAdminUserPermissionRepository>();
        services.AddScoped<IReadOnlyAdminUserRoleRepository, AdminUserRoleRepository>();
        services.AddScoped<IAdminUserRoleRepository, AdminUserRoleRepository>();
        services.AddScoped<IReadOnlyRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
    }

    private static void AddAuth(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLucidAspNetCorePasswordHashing();
        services.AddLucidCurrentUser();
        services.AddLucidJwtAuthentication(
            configuration.GetRequiredSection(JwtAccessTokenOptions.ConfigurationSectionName));
    }

    private static void AddSharedInfrastructure(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLucidRedisCaching(
            configuration.GetRequiredSection(LucidRedisCacheOptions.ConfigurationSectionName));
        services.AddLucidHttpResilience(
            configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));
        services.AddLucidConsulServiceDiscovery(
            configuration.GetRequiredSection(LucidConsulServiceDiscoveryOptions.ConfigurationSectionName));
        services.AddLucidConsulServiceRegistration(
            configuration.GetRequiredSection(LucidConsulServiceRegistrationOptions.ConfigurationSectionName));
        services.AddLucidConsulHealthCheck();
    }

    private static void AddMessaging(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddLucidRabbitMqEventBus(
            configuration.GetRequiredSection(RabbitMqEventBusOptions.ConfigurationSectionName));
        services.AddLucidOutboxPublisherHostedService();
        services.AddLucidRabbitMqHealthCheck();
    }

    private static void AddSmsLogin(
        IServiceCollection services,
        IConfiguration configuration)
    {
        AddSmsLoginRedisStore(
            services,
            configuration.GetRequiredSection(SmsLoginOptions.ConfigurationSectionName));
    }

    private static void AddNotificationClient(
        IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddHttpClient<INotificationClient, NotificationClient>()
            .AddLucidServiceDiscovery("notification")
            .AddLucidStandardHttpResilienceHandler(
                configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));
    }

    private static void AddSmsLoginRedisStore(
        IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        var options = configurationSection.Get<SmsLoginOptions>() ?? new SmsLoginOptions();
        options.Validate();

        services
            .AddOptions<SmsLoginOptions>()
            .Bind(configurationSection)
            .Validate(ValidateSmsLoginOptions, "Identity SMS login options are invalid.")
            .ValidateOnStart();

        services.AddSingleton(options);
        services.AddScoped<ISmsLoginCodeStore, RedisSmsLoginCodeStore>();
    }

    private static bool ValidateSmsLoginOptions(SmsLoginOptions options)
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
