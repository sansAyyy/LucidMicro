using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Auth.AspNetCore.Services;

public sealed class AspNetCorePasswordHashingService : IPasswordHashingService
{
    private static readonly object User = new();

    private readonly PasswordHasher<object> _passwordHasher;

    public AspNetCorePasswordHashingService(IOptions<PasswordHasherOptions> optionsAccessor)
    {
        _passwordHasher = new PasswordHasher<object>(optionsAccessor);
    }

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _passwordHasher.HashPassword(User, password);
    }

    public PasswordHashVerificationResult VerifyHashedPassword(string passwordHash, string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);

        var result = _passwordHasher.VerifyHashedPassword(User, passwordHash, providedPassword);

        return result switch
        {
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success => PasswordHashVerificationResult.Success,
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded => PasswordHashVerificationResult.SuccessRehashNeeded,
            _ => PasswordHashVerificationResult.Failed
        };
    }
}
