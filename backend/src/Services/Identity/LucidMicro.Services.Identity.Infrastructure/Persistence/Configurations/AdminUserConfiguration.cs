using LucidMicro.BuildingBlocks.Persistence.EFCore.Conventions;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence.Configurations;

public sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");

        builder.HasKey(adminUser => adminUser.Id);

        builder.Property(adminUser => adminUser.Id)
            .HasColumnName("id");

        builder.Property(adminUser => adminUser.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(adminUser => adminUser.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(adminUser => adminUser.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(adminUser => adminUser.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(32);

        builder.Property(adminUser => adminUser.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(adminUser => adminUser.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(adminUser => adminUser.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.HasIndex(adminUser => adminUser.UserName)
            .IsUnique()
            .HasDatabaseName("ix_admin_users_user_name")
            .HasSoftDeleteFilter();

        builder.HasIndex(adminUser => adminUser.Email)
            .IsUnique()
            .HasDatabaseName("ix_admin_users_email")
            .HasSoftDeleteFilter();

        builder.HasIndex(adminUser => adminUser.PhoneNumber)
            .IsUnique()
            .HasDatabaseName("ix_admin_users_phone_number")
            .HasSoftDeleteFilter();
    }
}
