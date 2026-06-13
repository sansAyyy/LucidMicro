using LucidMicro.BuildingBlocks.Outbox.EFCore.ModelBuilding;
using LucidMicro.BuildingBlocks.Persistence.EFCore.DbContexts;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using LucidMicro.Services.Identity.Domain.Entities.Permissions;
using LucidMicro.Services.Identity.Domain.Entities.Roles;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : LucidDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<AdminUserRole> AdminUserRoles => Set<AdminUserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        modelBuilder.ConfigureOutbox();

        base.OnModelCreating(modelBuilder);
    }
}
