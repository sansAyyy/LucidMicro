namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;

public sealed record RefreshAdminUserTokenRequest
{
    public string? RefreshToken { get; init; }
}
