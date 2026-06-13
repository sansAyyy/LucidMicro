using LucidMicro.BuildingBlocks.Domain.Core.Exceptions;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;

namespace LucidMicro.Services.Identity.Domain.Tests;

public sealed class PermissionTests
{
    [Fact]
    public void Create_NormalizesTextValues()
    {
        var permission = Permission.Create(
            Guid.NewGuid(),
            " identity.admin-users.read ",
            " View admin users ",
            " View admin user records. ",
            " identity ",
            " Identity ",
            " admin-users ",
            " Admin users ",
            " read ",
            sortOrder: 10,
            isEnabled: true);

        Assert.Equal("identity.admin-users.read", permission.Code);
        Assert.Equal("View admin users", permission.Name);
        Assert.Equal("View admin user records.", permission.Description);
        Assert.Equal("identity", permission.GroupCode);
        Assert.Equal("Identity", permission.GroupName);
        Assert.Equal("admin-users", permission.ResourceCode);
        Assert.Equal("Admin users", permission.ResourceName);
        Assert.Equal("read", permission.Action);
        Assert.Equal(10, permission.SortOrder);
        Assert.True(permission.IsEnabled);
    }

    [Fact]
    public void UpdateMetadata_ChangesDisplayFields_WithoutChangingCode()
    {
        var permission = CreatePermission();

        permission.UpdateMetadata(
            " Manage admin users ",
            " ",
            " identity ",
            " Identity ",
            " admin-users ",
            " Admin users ",
            " manage ",
            sortOrder: 20,
            isEnabled: false);

        Assert.Equal("identity.admin-users.read", permission.Code);
        Assert.Equal("Manage admin users", permission.Name);
        Assert.Null(permission.Description);
        Assert.Equal("manage", permission.Action);
        Assert.Equal(20, permission.SortOrder);
        Assert.False(permission.IsEnabled);
    }

    [Fact]
    public void Enable_AndDisable_UpdateEnabledState()
    {
        var permission = CreatePermission();

        permission.Disable();
        Assert.False(permission.IsEnabled);

        permission.Enable();
        Assert.True(permission.IsEnabled);
    }

    [Fact]
    public void Create_ThrowsDomainException_WhenRequiredTextIsBlank()
    {
        var exception = Assert.Throws<DomainException>(() => Permission.Create(
            Guid.NewGuid(),
            " ",
            "View admin users",
            null,
            "identity",
            "Identity",
            "admin-users",
            "Admin users",
            "read",
            sortOrder: 10,
            isEnabled: true));

        Assert.Equal("code is required.", exception.Message);
    }

    private static Permission CreatePermission()
    {
        return Permission.Create(
            Guid.NewGuid(),
            "identity.admin-users.read",
            "View admin users",
            null,
            "identity",
            "Identity",
            "admin-users",
            "Admin users",
            "read",
            sortOrder: 10,
            isEnabled: true);
    }
}
