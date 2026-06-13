using LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Contracts;

public interface IPasswordHashingService
{
    string HashPassword(string password);

    PasswordHashVerificationResult VerifyHashedPassword(string passwordHash, string providedPassword);
}
