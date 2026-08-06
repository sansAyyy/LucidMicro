using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;

public interface IAdminAccessTokenClaimsFactory
{
    Task<AccessTokenClaims> CreateAsync(
        AdminUser adminUser,
        CancellationToken cancellationToken = default);
}
