using LucidMicro.Services.Identity.Application.Features.Permissions.Services;
using LucidMicro.Services.Identity.Application.Tests.TestDoubles;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;

namespace LucidMicro.Services.Identity.Application.Tests;

public sealed class PermissionApplicationServiceTests
{
    [Fact]
    public async Task GetListAsync_ReturnsPermissionsOrderedBySortOrder()
    {
        var first = CreatePermission("identity.roles.read", sortOrder: 20);
        var second = CreatePermission("identity.admin-users.read", sortOrder: 10);
        var service = new PermissionApplicationService(new InMemoryPermissionRepository([first, second]));

        var result = await service.GetListAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal([second.Id, first.Id], result.Value.Select(permission => permission.Id));
    }

    private static Permission CreatePermission(string code, int sortOrder)
    {
        return Permission.Create(
            Guid.NewGuid(),
            code,
            code,
            null,
            "identity",
            "Identity",
            "roles",
            "Roles",
            "read",
            sortOrder,
            isEnabled: true);
    }
}
