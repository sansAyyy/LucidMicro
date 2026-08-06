using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Services;

public sealed class AdminAccessTokenClaimsFactory : IAdminAccessTokenClaimsFactory
{
    private readonly IReadOnlyAdminUserPermissionRepository _permissions;

    public AdminAccessTokenClaimsFactory(IReadOnlyAdminUserPermissionRepository permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        _permissions = permissions;
    }

    public async Task<AccessTokenClaims> CreateAsync(
        AdminUser adminUser,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adminUser);

        var permissionCodes = await _permissions.GetPermissionCodesAsync(
            adminUser.Id,
            cancellationToken);

        var claims = new AccessTokenClaims(
            adminUser.Id.ToString(),
            adminUser.UserName,
            new Dictionary<string, string>
            {
                ["email"] = adminUser.Email,
                [LucidClaimTypes.AuthorizationVersion] = "1"
            })
        {
            AdditionalClaimValues = permissionCodes
                .Select(permission => new AccessTokenClaim(LucidClaimTypes.Permission, permission))
                .ToArray()
        };

        return claims;
    }
}
