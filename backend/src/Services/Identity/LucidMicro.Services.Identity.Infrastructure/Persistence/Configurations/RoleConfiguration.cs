using LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;
using LucidMicro.Services.Identity.Domain.Entities.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Id)
            .HasColumnName("id");

        builder.Property(role => role.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(role => role.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(role => role.Description)
            .HasColumnName("description")
            .HasMaxLength(512);

        builder.Property(role => role.IsSystem)
            .HasColumnName("is_system")
            .IsRequired();

        builder.Property(role => role.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.HasIndex(role => role.Code)
            .IsUnique()
            .HasDatabaseName("ix_roles_code")
            .HasSoftDeleteFilter();
    }
}
