using LucidMicro.BuildingBlocks.Application.Results;

namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Errors;

internal static class SmsLoginErrors
{
    public const string ValidationErrorCode = "Identity.SmsLogin.Validation";

    public static Error TooManyRequests()
    {
        return Error.Conflict("Identity.SmsLogin.TooManyRequests", "SMS login code was sent too frequently.");
    }

    public static Error NotificationUnavailable()
    {
        return Error.Failure("Identity.SmsLogin.NotificationUnavailable", "SMS login code notification failed.");
    }

    public static Error CodeExpired()
    {
        return Error.Failure("Identity.SmsLogin.CodeExpired", "SMS login code expired.");
    }

    public static Error InvalidCode()
    {
        return Error.Failure("Identity.SmsLogin.InvalidCode", "SMS login code is invalid.");
    }

    public static Error TooManyAttempts()
    {
        return Error.Conflict("Identity.SmsLogin.TooManyAttempts", "SMS login code attempts exceeded.");
    }

    public static Error InvalidCredentials()
    {
        return Error.Unauthorized("Identity.SmsLogin.InvalidCredentials", "Invalid SMS login credentials.");
    }

    public static Error Disabled()
    {
        return Error.Forbidden("Identity.SmsLogin.Disabled", "Admin user is disabled.");
    }
}
