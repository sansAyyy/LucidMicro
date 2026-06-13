using LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Responses;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Repositories;

public sealed class AdminUserRoleRepository : IAdminUserRoleRepository
{
    private readonly IdentityDbContext _dbContext;

    public AdminUserRoleRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AdminUserRoleResponse>> GetRolesAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AdminUserRoles
            .AsNoTracking()
            .Where(adminUserRole => adminUserRole.AdminUserId == adminUserId)
            .Join(
                _dbContext.Roles.AsNoTracking(),
                adminUserRole => adminUserRole.RoleId,
                role => role.Id,
                (adminUserRole, role) => role)
            .OrderBy(role => role.Code)
            .Select(role => new AdminUserRoleResponse(
                role.Id,
                role.Code,
                role.Name,
                role.IsEnabled))
            .ToArrayAsync(cancellationToken);
    }

    public async Task ReplaceRolesAsync(
        Guid adminUserId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var existingAdminUserRoles = await _dbContext.AdminUserRoles
            .Where(adminUserRole => adminUserRole.AdminUserId == adminUserId)
            .ToArrayAsync(cancellationToken);

        _dbContext.AdminUserRoles.RemoveRange(existingAdminUserRoles);

        var newAdminUserRoles = roleIds
            .Distinct()
            .Select(roleId => AdminUserRole.Create(adminUserId, roleId));

        await _dbContext.AdminUserRoles.AddRangeAsync(newAdminUserRoles, cancellationToken);
    }
}
