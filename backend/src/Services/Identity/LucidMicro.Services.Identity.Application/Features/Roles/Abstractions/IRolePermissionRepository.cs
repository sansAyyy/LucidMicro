namespace LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;

public interface IRolePermissionRepository : IReadOnlyRolePermissionRepository
{
    Task ReplacePermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken = default);
}
