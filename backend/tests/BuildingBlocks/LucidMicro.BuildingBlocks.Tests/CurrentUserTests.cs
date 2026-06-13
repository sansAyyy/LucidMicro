using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Auditing;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Services;
using Microsoft.AspNetCore.Http;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class CurrentUserTests
{
    [Fact]
    public void HttpContextCurrentUser_ReturnsAuthenticatedUserClaims()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, "admin-id"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim("email", "admin@example.com")
            ], "Test"))
        };
        var currentUser = new HttpContextCurrentUser(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal("admin-id", currentUser.UserId);
        Assert.Equal("admin", currentUser.UserName);
        Assert.Equal("admin@example.com", currentUser.Email);
    }

    [Fact]
    public void HttpContextCurrentUser_ReturnsEmptyValues_WhenUserIsNotAuthenticated()
    {
        var currentUser = new HttpContextCurrentUser(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        Assert.False(currentUser.IsAuthenticated);
        Assert.Null(currentUser.UserId);
        Assert.Null(currentUser.UserName);
        Assert.Null(currentUser.Email);
    }

    [Fact]
    public void CurrentUserAuditUserProvider_ReturnsCurrentUserId_WhenUserIsAuthenticated()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "admin-id")
            ], "Test"))
        };
        var currentUser = new HttpContextCurrentUser(new HttpContextAccessor
        {
            HttpContext = httpContext
        });
        var auditUserProvider = new CurrentUserAuditUserProvider(currentUser);

        Assert.Equal("admin-id", auditUserProvider.GetCurrentUserId());
    }
}
