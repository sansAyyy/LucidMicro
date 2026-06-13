using LucidMicro.BuildingBlocks.Application.Results;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Errors;

internal static class AdminAuthErrors
{
    public const string ValidationErrorCode = "Identity.AdminAuth.Validation";

    public static Error InvalidCredentials()
    {
        return Error.Unauthorized("Identity.AdminAuth.InvalidCredentials", "Invalid credentials.");
    }

    public static Error InvalidRefreshToken()
    {
        return Error.Unauthorized("Identity.AdminAuth.InvalidRefreshToken", "Invalid refresh token.");
    }

    public static Error InvalidCurrentUser()
    {
        return Error.Unauthorized("Identity.AdminAuth.InvalidCurrentUser", "Current admin user is invalid.");
    }

    public static Error Disabled()
    {
        return Error.Forbidden("Identity.AdminAuth.Disabled", "Admin user is disabled.");
    }
}
