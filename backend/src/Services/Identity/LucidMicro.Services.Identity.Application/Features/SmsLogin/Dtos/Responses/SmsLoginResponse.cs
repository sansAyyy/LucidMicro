namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Responses;

public sealed record SmsLoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
