using LucidMicro.Services.Identity.Application.Features.AdminUsers.Abstractions;
using LucidMicro.Services.Identity.Application.Features.AdminUsers.Dtos.Responses;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class TestAdminUserRoleRepository : IAdminUserRoleRepository
{
    private readonly Dictionary<Guid, List<Guid>> _roleIdsByAdminUserId = [];
    private readonly Dictionary<Guid, List<AdminUserRoleResponse>> _rolesByAdminUserId = [];

    public IReadOnlyDictionary<Guid, List<Guid>> RoleIdsByAdminUserId => _roleIdsByAdminUserId;

    public int ReplaceCount { get; private set; }

    public Task<IReadOnlyList<AdminUserRoleResponse>> GetRolesAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var roles = _rolesByAdminUserId.TryGetValue(adminUserId, out var existingRoles)
            ? existingRoles.ToArray()
            : [];

        return Task.FromResult<IReadOnlyList<AdminUserRoleResponse>>(roles);
    }

    public Task ReplaceRolesAsync(
        Guid adminUserId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        ReplaceCount++;
        _roleIdsByAdminUserId[adminUserId] = roleIds.ToList();
        _rolesByAdminUserId[adminUserId] = roleIds
            .Select(roleId => new AdminUserRoleResponse(roleId, string.Empty, string.Empty, true))
            .ToList();

        return Task.CompletedTask;
    }
}
