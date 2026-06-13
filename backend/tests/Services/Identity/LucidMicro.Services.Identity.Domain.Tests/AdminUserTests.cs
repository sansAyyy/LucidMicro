using LucidMicro.BuildingBlocks.Domain.Core.Exceptions;
using LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

namespace LucidMicro.Services.Identity.Domain.Tests;

public sealed class AdminUserTests
{
    [Fact]
    public void Create_NormalizesTextValues()
    {
        var adminUser = AdminUser.Create(
            Guid.NewGuid(),
            " admin ",
            " admin@example.com ",
            " Admin ",
            " ",
            " password-hash ",
            isActive: true);

        Assert.Equal("admin", adminUser.UserName);
        Assert.Equal("admin@example.com", adminUser.Email);
        Assert.Equal("Admin", adminUser.DisplayName);
        Assert.Null(adminUser.PhoneNumber);
        Assert.Equal("password-hash", adminUser.PasswordHash);
        Assert.True(adminUser.IsActive);
    }

    [Fact]
    public void Update_ChangesProfile_AndActiveState()
    {
        var adminUser = CreateAdminUser();

        adminUser.Update(" new-admin ", " new@example.com ", " New Admin ", " 123 ", isActive: false);

        Assert.Equal("new-admin", adminUser.UserName);
        Assert.Equal("new@example.com", adminUser.Email);
        Assert.Equal("New Admin", adminUser.DisplayName);
        Assert.Equal("123", adminUser.PhoneNumber);
        Assert.False(adminUser.IsActive);
    }

    [Fact]
    public void Activate_AndDeactivate_UpdateActiveState()
    {
        var adminUser = CreateAdminUser();

        adminUser.Deactivate();
        Assert.False(adminUser.IsActive);

        adminUser.Activate();
        Assert.True(adminUser.IsActive);
    }

    [Fact]
    public void ChangePassword_NormalizesPasswordHash()
    {
        var adminUser = CreateAdminUser();

        adminUser.ChangePassword(" new-hash ");

        Assert.Equal("new-hash", adminUser.PasswordHash);
    }

    [Fact]
    public void MarkLogin_UpdatesLastLoginAt()
    {
        var adminUser = CreateAdminUser();
        var lastLoginAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

        adminUser.MarkLogin(lastLoginAt);

        Assert.Equal(lastLoginAt, adminUser.LastLoginAt);
    }

    [Fact]
    public void Create_ThrowsDomainException_WhenRequiredTextIsBlank()
    {
        var exception = Assert.Throws<DomainException>(() => AdminUser.Create(
            Guid.NewGuid(),
            " ",
            "admin@example.com",
            "Admin",
            null,
            "password-hash",
            isActive: true));

        Assert.Equal("userName is required.", exception.Message);
    }

    [Fact]
    public void Create_ThrowsDomainException_WhenTextExceedsMaxLength()
    {
        var exception = Assert.Throws<DomainException>(() => AdminUser.Create(
            Guid.NewGuid(),
            new string('a', 65),
            "admin@example.com",
            "Admin",
            null,
            "password-hash",
            isActive: true));

        Assert.Equal("userName exceeds max length 64.", exception.Message);
    }

    private static AdminUser CreateAdminUser()
    {
        return AdminUser.Create(
            Guid.NewGuid(),
            "admin",
            "admin@example.com",
            "Admin",
            null,
            "password-hash",
            isActive: true);
    }
}
