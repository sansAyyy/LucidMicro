using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using LucidMicro.BuildingBlocks.Auth.AspNetCore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class AspNetCorePasswordHashingServiceTests
{
    [Fact]
    public void HashPassword_ReturnsHashThatCanBeVerified()
    {
        var service = CreateService();
        const string password = "P@ssw0rd!";

        var passwordHash = service.HashPassword(password);

        Assert.NotEqual(password, passwordHash);
        Assert.Equal(
            PasswordHashVerificationResult.Success,
            service.VerifyHashedPassword(passwordHash, password));
    }

    [Fact]
    public void VerifyHashedPassword_ReturnsFailed_WhenPasswordDoesNotMatch()
    {
        var service = CreateService();
        var passwordHash = service.HashPassword("P@ssw0rd!");

        var result = service.VerifyHashedPassword(passwordHash, "wrong-password");

        Assert.Equal(PasswordHashVerificationResult.Failed, result);
    }

    private static AspNetCorePasswordHashingService CreateService()
    {
        return new AspNetCorePasswordHashingService(Options.Create(new PasswordHasherOptions()));
    }
}
