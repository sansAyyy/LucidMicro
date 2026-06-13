using LucidMicro.Services.Identity.Domain.Entities.Permissions;
using LucidMicro.Services.Identity.Domain.Entities.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rolePermission => new
        {
            rolePermission.RoleId,
            rolePermission.PermissionId
        });

        builder.Property(rolePermission => rolePermission.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(rolePermission => rolePermission.PermissionId)
            .HasColumnName("permission_id")
            .IsRequired();

        builder.HasIndex(rolePermission => rolePermission.PermissionId)
            .HasDatabaseName("ix_role_permissions_permission_id");

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(rolePermission => rolePermission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(rolePermission => rolePermission.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
