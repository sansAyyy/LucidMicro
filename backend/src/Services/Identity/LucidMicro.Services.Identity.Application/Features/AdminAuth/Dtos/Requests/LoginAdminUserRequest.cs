namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;

public sealed record LoginAdminUserRequest
{
    public string? LoginName { get; init; }

    public string? Password { get; init; }
}
