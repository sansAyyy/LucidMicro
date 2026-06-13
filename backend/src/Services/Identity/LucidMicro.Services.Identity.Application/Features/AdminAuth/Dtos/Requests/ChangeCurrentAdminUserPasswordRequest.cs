namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;

public sealed record ChangeCurrentAdminUserPasswordRequest
{
    public string? CurrentPassword { get; init; }

    public string? NewPassword { get; init; }

    public string? ConfirmPassword { get; init; }
}
