using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Responses;

public sealed record CurrentAdminUserResponse(
    Guid Id,
    string UserName,
    string Email,
    string DisplayName,
    string? PhoneNumber,
    bool IsActive,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Permissions)
{
    public static CurrentAdminUserResponse FromEntity(
        AdminUser adminUser,
        IReadOnlyList<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(adminUser);
        ArgumentNullException.ThrowIfNull(permissions);

        return new CurrentAdminUserResponse(
            adminUser.Id,
            adminUser.UserName,
            adminUser.Email,
            adminUser.DisplayName,
            adminUser.PhoneNumber,
            adminUser.IsActive,
            adminUser.LastLoginAt,
            permissions);
    }
}
