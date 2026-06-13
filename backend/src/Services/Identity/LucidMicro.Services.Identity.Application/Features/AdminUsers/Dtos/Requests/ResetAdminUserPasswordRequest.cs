namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;

public sealed record ResetAdminUserPasswordRequest
{
    public string? NewPassword { get; init; }

    public string? ConfirmPassword { get; init; }
}
