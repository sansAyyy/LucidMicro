using LucidMicro.BuildingBlocks.Auth.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.DependencyInjection;

public static class PermissionAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddLucidPermissionAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization();
        services.Replace(
            ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());

        return services;
    }
}
