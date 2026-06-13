using LucidMicro.Services.Identity.Application.Features.Roles.Abstractions;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class TestRolePermissionRepository : IRolePermissionRepository
{
    private readonly Dictionary<Guid, List<Guid>> _permissionIdsByRoleId = [];

    public IReadOnlyDictionary<Guid, List<Guid>> PermissionIdsByRoleId => _permissionIdsByRoleId;

    public int ReplaceCount { get; private set; }

    public Task<IReadOnlyList<Guid>> GetPermissionIdsAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var permissionIds = _permissionIdsByRoleId.TryGetValue(roleId, out var existingPermissionIds)
            ? existingPermissionIds.ToArray()
            : [];

        return Task.FromResult<IReadOnlyList<Guid>>(permissionIds);
    }

    public Task ReplacePermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken = default)
    {
        ReplaceCount++;
        _permissionIdsByRoleId[roleId] = permissionIds.ToList();

        return Task.CompletedTask;
    }

    public void SetPermissions(Guid roleId, params Guid[] permissionIds)
    {
        _permissionIdsByRoleId[roleId] = permissionIds.ToList();
    }
}
