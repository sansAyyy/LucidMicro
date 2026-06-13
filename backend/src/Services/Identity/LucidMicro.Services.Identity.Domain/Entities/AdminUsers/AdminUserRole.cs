namespace LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

public class AdminUserRole
{
    private AdminUserRole()
    {
    }

    private AdminUserRole(Guid adminUserId, Guid roleId)
    {
        AdminUserId = adminUserId;
        RoleId = roleId;
    }

    public Guid AdminUserId { get; private set; }

    public Guid RoleId { get; private set; }

    public static AdminUserRole Create(Guid adminUserId, Guid roleId)
    {
        return new AdminUserRole(adminUserId, roleId);
    }
}
