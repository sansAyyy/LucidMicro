namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Responses;

public sealed record LoginAdminUserResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
