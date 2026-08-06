using System.Security.Claims;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public async Task Handler_Succeeds_WhenPermissionClaimExists()
    {
        var requirement = new PermissionRequirement("identity.admin-users.read");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(LucidClaimTypes.Permission, requirement.Permission)],
        "Bearer"));
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_DoesNotSucceed_WhenPermissionClaimIsMissing()
    {
        var requirement = new PermissionRequirement("identity.admin-users.read");
        var context = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity("Bearer")),
            null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PolicyProvider_CreatesAuthenticatedPermissionPolicy()
    {
        var provider = new PermissionAuthorizationPolicyProvider(
            Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync(
            $"{RequirePermissionAttribute.PolicyPrefix}identity.admin-users.read");

        Assert.NotNull(policy);
        Assert.Contains(policy!.Requirements, requirement =>
            requirement is PermissionRequirement permission
            && permission.Permission == "identity.admin-users.read");
    }
}
