using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Auditing;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Options;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Services;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Auditing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidAspNetCorePasswordHashing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<PasswordHasherOptions>();
        services.TryAddScoped<IPasswordHashingService, AspNetCorePasswordHashingService>();

        return services;
    }

    public static IServiceCollection AddLucidCurrentUser(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.TryAddScoped<IAuditUserProvider, CurrentUserAuditUserProvider>();

        return services;
    }

    public static IServiceCollection AddLucidJwtAuthentication(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(configurationSection);

        return services.AddLucidJwtAuthentication(options => configurationSection.Bind(options));
    }

    public static IServiceCollection AddLucidJwtAuthentication(
        this IServiceCollection services,
        Action<JwtAccessTokenOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new JwtAccessTokenOptions();
        configureOptions(options);
        ApplyDefaults(options);
        options.Validate();

        services.TryAddSingleton(TimeProvider.System);
        services
            .AddOptions<JwtAccessTokenOptions>()
            .Configure(options =>
            {
                configureOptions(options);
                ApplyDefaults(options);
            })
            .Validate(ValidateOptions, "Lucid JWT access token options are invalid.")
            .ValidateOnStart();
        services.TryAddScoped<IAccessTokenService, JwtAccessTokenService>();
        services.TryAddScoped<IRefreshTokenService, JwtAccessTokenService>();
        services.TryAddScoped<IRefreshTokenValidator, JwtAccessTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtBearerOptions =>
            {
                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        return services;
    }

    private static void ApplyDefaults(JwtAccessTokenOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RefreshAudience) && !string.IsNullOrWhiteSpace(options.Audience))
        {
            options.RefreshAudience = $"{options.Audience}.Refresh";
        }
    }

    private static bool ValidateOptions(JwtAccessTokenOptions options)
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
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
