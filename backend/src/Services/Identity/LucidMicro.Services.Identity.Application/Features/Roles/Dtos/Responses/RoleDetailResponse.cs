using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Responses;

public sealed record RoleDetailResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsEnabled,
    IReadOnlyList<Guid> PermissionIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt)
{
    public static RoleDetailResponse FromEntity(Role role, IReadOnlyList<Guid> permissionIds)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permissionIds);

        return new RoleDetailResponse(
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            role.IsSystem,
            role.IsEnabled,
            permissionIds,
            role.CreatedAt,
            role.LastModifiedAt);
    }
}
