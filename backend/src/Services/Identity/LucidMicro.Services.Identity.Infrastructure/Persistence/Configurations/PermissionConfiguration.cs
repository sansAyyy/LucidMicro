using LucidMicro.Services.Identity.Domain.Entities.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .HasColumnName("id");

        builder.Property(permission => permission.Code)
            .HasColumnName("code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(permission => permission.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(permission => permission.Description)
            .HasColumnName("description")
            .HasMaxLength(512);

        builder.Property(permission => permission.GroupCode)
            .HasColumnName("group_code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(permission => permission.GroupName)
            .HasColumnName("group_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(permission => permission.ResourceCode)
            .HasColumnName("resource_code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(permission => permission.ResourceName)
            .HasColumnName("resource_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(permission => permission.Action)
            .HasColumnName("action")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(permission => permission.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(permission => permission.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.HasIndex(permission => permission.Code)
            .IsUnique()
            .HasDatabaseName("ix_permissions_code");

        builder.HasIndex(permission => new
            {
                permission.GroupCode,
                permission.ResourceCode,
                permission.Action
            })
            .IsUnique()
            .HasDatabaseName("ix_permissions_group_resource_action");
    }
}
