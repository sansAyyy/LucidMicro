using FluentValidation;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Services;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Services;
using LucidMicro.Services.Identity.Application.Features.Permissions.Abstractions;
using LucidMicro.Services.Identity.Application.Features.Permissions.Services;
using LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;
using LucidMicro.Services.Identity.Application.Features.Roles.Services;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Abstractions;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LucidMicro.Services.Identity.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAdminAuthApplicationService, AdminAuthApplicationService>();
        services.AddScoped<IAdminUserApplicationService, AdminUserApplicationService>();
        services.AddScoped<IPermissionApplicationService, PermissionApplicationService>();
        services.AddScoped<IRoleApplicationService, RoleApplicationService>();
        services.TryAddScoped<ISmsLoginCodeGenerator, RandomSmsLoginCodeGenerator>();
        services.AddScoped<ISmsLoginApplicationService, SmsLoginApplicationService>();
        services.AddValidatorsFromAssemblyContaining<AdminUserApplicationService>();

        return services;
    }
}
