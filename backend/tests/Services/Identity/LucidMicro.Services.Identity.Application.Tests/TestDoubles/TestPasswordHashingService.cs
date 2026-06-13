using LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

namespace LucidMicro.Services.Identity.Application.Tests.TestDoubles;

internal sealed class TestPasswordHashingService : IPasswordHashingService
{
    public int HashCount { get; private set; }

    public string HashPassword(string password)
    {
        HashCount++;

        return $"hashed:{password}";
    }

    public PasswordHashVerificationResult VerifyHashedPassword(string passwordHash, string providedPassword)
    {
        if (passwordHash == $"hashed:{providedPassword}")
        {
            return PasswordHashVerificationResult.Success;
        }

        if (passwordHash == $"legacy:{providedPassword}")
        {
            return PasswordHashVerificationResult.SuccessRehashNeeded;
        }

        return PasswordHashVerificationResult.Failed;
    }
}
