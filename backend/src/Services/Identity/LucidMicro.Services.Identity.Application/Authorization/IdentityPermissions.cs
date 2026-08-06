namespace LucidMicro.Services.Identity.Application.Authorization;

public static class IdentityPermissions
{
    public const string AdminUsersRead = "identity.admin-users.read";
    public const string AdminUsersCreate = "identity.admin-users.create";
    public const string AdminUsersUpdate = "identity.admin-users.update";
    public const string AdminUsersEnable = "identity.admin-users.enable";
    public const string AdminUsersDisable = "identity.admin-users.disable";
    public const string AdminUsersResetPassword = "identity.admin-users.reset-password";
    public const string AdminUsersDelete = "identity.admin-users.delete";
    public const string RolesRead = "identity.roles.read";
    public const string RolesManage = "identity.roles.manage";
    public const string RolesAssignPermissions = "identity.roles.assign-permissions";
}
