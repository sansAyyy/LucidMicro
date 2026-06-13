namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Requests;

public sealed record UpdateAdminUserRequest
{
    public string? UserName { get; init; }

    public string? Email { get; init; }

    public string? DisplayName { get; init; }

    public string? PhoneNumber { get; init; }

    public bool IsActive { get; init; } = true;
}
