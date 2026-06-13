using LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;
using LucidMicro.Services.Identity.Domain.Entities.Roles;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Repositories;

public sealed class RolePermissionRepository : IRolePermissionRepository
{
    private readonly IdentityDbContext _dbContext;

    public RolePermissionRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Guid>> GetPermissionIdsAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .Select(rolePermission => rolePermission.PermissionId)
            .OrderBy(permissionId => permissionId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task ReplacePermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissionIds);

        var existingRolePermissions = await _dbContext.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .ToArrayAsync(cancellationToken);

        _dbContext.RolePermissions.RemoveRange(existingRolePermissions);

        var newRolePermissions = permissionIds
            .Distinct()
            .Select(permissionId => RolePermission.Create(roleId, permissionId));

        await _dbContext.RolePermissions.AddRangeAsync(newRolePermissions, cancellationToken);
    }
}
