using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Responses;

public sealed record AdminUserRoleResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsEnabled)
{
    public static AdminUserRoleResponse FromEntity(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new AdminUserRoleResponse(
            role.Id,
            role.Code,
            role.Name,
            role.IsEnabled);
    }
}

public sealed record AdminUserResponse(
    Guid Id,
    string UserName,
    string Email,
    string DisplayName,
    string? PhoneNumber,
    bool IsActive,
    DateTime? LastLoginAt,
    IReadOnlyList<AdminUserRoleResponse> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt)
{
    public static AdminUserResponse FromEntity(
        AdminUser adminUser,
        IReadOnlyList<AdminUserRoleResponse>? roles = null)
    {
        ArgumentNullException.ThrowIfNull(adminUser);

        return new AdminUserResponse(
            adminUser.Id,
            adminUser.UserName,
            adminUser.Email,
            adminUser.DisplayName,
            adminUser.PhoneNumber,
            adminUser.IsActive,
            adminUser.LastLoginAt,
            roles ?? [],
            adminUser.CreatedAt,
            adminUser.LastModifiedAt);
    }
}
