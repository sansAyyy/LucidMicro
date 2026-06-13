using LucidMicro.Services.Identity.Application.Features.AdminAuth.Abstractions;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class TestAdminUserPermissionRepository : IReadOnlyAdminUserPermissionRepository
{
    private readonly Dictionary<Guid, List<string>> _permissionCodesByAdminUserId = [];

    public int GetPermissionCodesCount { get; private set; }

    public Task<IReadOnlyList<string>> GetPermissionCodesAsync(
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        GetPermissionCodesCount++;
        var permissionCodes = _permissionCodesByAdminUserId.TryGetValue(adminUserId, out var existingPermissionCodes)
            ? existingPermissionCodes.ToArray()
            : [];

        return Task.FromResult<IReadOnlyList<string>>(permissionCodes);
    }

    public void SetPermissions(Guid adminUserId, params string[] permissionCodes)
    {
        _permissionCodesByAdminUserId[adminUserId] = permissionCodes.ToList();
    }
}
