using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;

public interface IAccessTokenService
{
    AccessToken GenerateAccessToken(AccessTokenClaims claims);
}
