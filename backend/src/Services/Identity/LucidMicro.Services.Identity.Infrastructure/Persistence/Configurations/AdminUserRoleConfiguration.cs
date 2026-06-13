using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using LucidMicro.Services.Identity.Domain.Entities.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Configurations;

public sealed class AdminUserRoleConfiguration : IEntityTypeConfiguration<AdminUserRole>
{
    public void Configure(EntityTypeBuilder<AdminUserRole> builder)
    {
        builder.ToTable("admin_user_roles");

        builder.HasKey(adminUserRole => new
        {
            adminUserRole.AdminUserId,
            adminUserRole.RoleId
        });

        builder.Property(adminUserRole => adminUserRole.AdminUserId)
            .HasColumnName("admin_user_id")
            .IsRequired();

        builder.Property(adminUserRole => adminUserRole.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.HasIndex(adminUserRole => adminUserRole.RoleId)
            .HasDatabaseName("ix_admin_user_roles_role_id");

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(adminUserRole => adminUserRole.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(adminUserRole => adminUserRole.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
