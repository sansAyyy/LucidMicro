using LucidMicro.BuildingBlocks.Domain.Core.Exceptions;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using LucidMicro.Services.Identity.Domain.Entities.Roles;

namespace LucidMicro.Services.Identity.Domain.Tests;

public sealed class RoleTests
{
    [Fact]
    public void Create_NormalizesTextValues()
    {
        var role = Role.Create(
            Guid.NewGuid(),
            " super-admin ",
            " Super Admin ",
            " Built-in administrator role. ",
            isSystem: true,
            isEnabled: true);

        Assert.Equal("super-admin", role.Code);
        Assert.Equal("Super Admin", role.Name);
        Assert.Equal("Built-in administrator role.", role.Description);
        Assert.True(role.IsSystem);
        Assert.True(role.IsEnabled);
    }

    [Fact]
    public void Update_ChangesProfile_WithoutChangingCodeOrSystemFlag()
    {
        var role = CreateRole();

        role.Update(" Operator ", " ", isEnabled: false);

        Assert.Equal("super-admin", role.Code);
        Assert.Equal("Operator", role.Name);
        Assert.Null(role.Description);
        Assert.True(role.IsSystem);
        Assert.False(role.IsEnabled);
    }

    [Fact]
    public void Enable_AndDisable_UpdateEnabledState()
    {
        var role = CreateRole();

        role.Disable();
        Assert.False(role.IsEnabled);

        role.Enable();
        Assert.True(role.IsEnabled);
    }

    [Fact]
    public void Create_ThrowsDomainException_WhenRequiredTextIsBlank()
    {
        var exception = Assert.Throws<DomainException>(() => Role.Create(
            Guid.NewGuid(),
            " ",
            "Super Admin",
            null,
            isSystem: true,
            isEnabled: true));

        Assert.Equal("code is required.", exception.Message);
    }

    [Fact]
    public void RolePermission_Create_StoresRelationIds()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var rolePermission = RolePermission.Create(roleId, permissionId);

        Assert.Equal(roleId, rolePermission.RoleId);
        Assert.Equal(permissionId, rolePermission.PermissionId);
    }

    [Fact]
    public void AdminUserRole_Create_StoresRelationIds()
    {
        var adminUserId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var adminUserRole = AdminUserRole.Create(adminUserId, roleId);

        Assert.Equal(adminUserId, adminUserRole.AdminUserId);
        Assert.Equal(roleId, adminUserRole.RoleId);
    }

    private static Role CreateRole()
    {
        return Role.Create(
            Guid.NewGuid(),
            "super-admin",
            "Super Admin",
            "Built-in administrator role.",
            isSystem: true,
            isEnabled: true);
    }
}
