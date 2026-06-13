using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Responses;

public sealed record RoleResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt)
{
    public static RoleResponse FromEntity(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new RoleResponse(
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            role.IsSystem,
            role.IsEnabled,
            role.CreatedAt,
            role.LastModifiedAt);
    }
}
