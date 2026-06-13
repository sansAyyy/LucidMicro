using LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Repositories;

public sealed class ReadOnlyAdminUserPermissionRepository : IReadOnlyAdminUserPermissionRepository
{
    private readonly IdentityDbContext _dbContext;

    public ReadOnlyAdminUserPermissionRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var permissionCodes =
            from adminUserRole in _dbContext.AdminUserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on adminUserRole.RoleId equals role.Id
            join rolePermission in _dbContext.RolePermissions.AsNoTracking() on role.Id equals rolePermission.RoleId
            join permission in _dbContext.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
            where adminUserRole.AdminUserId == adminUserId
                  && role.IsEnabled
                  && permission.IsEnabled
            select permission.Code;

        return await permissionCodes
            .Distinct()
            .OrderBy(code => code)
            .ToArrayAsync(cancellationToken);
    }
}
