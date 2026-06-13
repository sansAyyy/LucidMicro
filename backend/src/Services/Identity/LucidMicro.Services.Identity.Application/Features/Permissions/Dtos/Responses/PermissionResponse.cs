using LucidMicro.Services.Identity.Domain.Entities.Permissions;

namespace LucidMicro.Services.Identity.Application.Features.Permissions.Dtos.Responses;

public sealed record PermissionResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string GroupCode,
    string GroupName,
    string ResourceCode,
    string ResourceName,
    string Action,
    int SortOrder,
    bool IsEnabled)
{
    public static PermissionResponse FromEntity(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return new PermissionResponse(
            permission.Id,
            permission.Code,
            permission.Name,
            permission.Description,
            permission.GroupCode,
            permission.GroupName,
            permission.ResourceCode,
            permission.ResourceName,
            permission.Action,
            permission.SortOrder,
            permission.IsEnabled);
    }
}
