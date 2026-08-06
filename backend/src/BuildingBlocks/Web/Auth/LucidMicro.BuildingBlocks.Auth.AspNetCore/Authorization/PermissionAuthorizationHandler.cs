using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using Microsoft.AspNetCore.Authorization;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(LucidClaimTypes.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
