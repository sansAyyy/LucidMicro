using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;

public interface IRefreshTokenService
{
    RefreshToken GenerateRefreshToken(AccessTokenClaims claims);
}
