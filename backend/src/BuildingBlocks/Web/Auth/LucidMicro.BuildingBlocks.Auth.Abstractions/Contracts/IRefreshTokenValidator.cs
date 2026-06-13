using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;

public interface IRefreshTokenValidator
{
    AccessTokenClaims? ValidateRefreshToken(string refreshToken);
}
