namespace LucidMicro.BuildingBlocks.Auth.Abstractions.Models;

public enum PasswordHashVerificationResult
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2
}
