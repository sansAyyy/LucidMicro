using LucidMicro.Services.Identity.Api.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace LucidMicro.Services.Identity.Api.Tests;

public sealed class ControllerAuthorizationTests
{
    [Fact]
    public void AdminUsersController_RequiresAuthenticatedUser()
    {
        var authorizeAttribute = typeof(AdminUsersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void AdminUsersController_ResetPassword_DoesNotAllowAnonymousAccess()
    {
        var method = typeof(AdminUsersController).GetMethod(nameof(AdminUsersController.ResetPassword));
        Assert.NotNull(method);

        var allowAnonymousAttribute = method
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .SingleOrDefault();

        Assert.Null(allowAnonymousAttribute);
    }

    [Fact]
    public void AdminAuthController_Login_AllowsAnonymousAccess()
    {
        var method = typeof(AdminAuthController).GetMethod(nameof(AdminAuthController.LoginAsync));
        Assert.NotNull(method);

        var allowAnonymousAttribute = method
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .SingleOrDefault();

        Assert.NotNull(allowAnonymousAttribute);
    }

    [Fact]
    public void AdminAuthController_Refresh_AllowsAnonymousAccess()
    {
        var method = typeof(AdminAuthController).GetMethod(nameof(AdminAuthController.RefreshAsync));
        Assert.NotNull(method);

        var allowAnonymousAttribute = method
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .SingleOrDefault();

        Assert.NotNull(allowAnonymousAttribute);
    }

    [Fact]
    public void AdminAuthController_GetCurrent_RequiresAuthenticatedUser()
    {
        var method = typeof(AdminAuthController).GetMethod(nameof(AdminAuthController.GetCurrentAsync));
        Assert.NotNull(method);

        var authorizeAttribute = method
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void AdminAuthController_ChangeCurrentPassword_RequiresAuthenticatedUser()
    {
        var method = typeof(AdminAuthController).GetMethod(nameof(AdminAuthController.ChangeCurrentPasswordAsync));
        Assert.NotNull(method);

        var authorizeAttribute = method
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void SmsLoginController_AllowsAnonymousAccess()
    {
        var allowAnonymousAttribute = typeof(SmsLoginController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .SingleOrDefault();

        Assert.NotNull(allowAnonymousAttribute);
    }
}
