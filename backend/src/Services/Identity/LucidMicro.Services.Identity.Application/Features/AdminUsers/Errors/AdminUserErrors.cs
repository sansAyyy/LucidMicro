using LucidMicro.BuildingBlocks.Application.Results;

namespace LucidMicro.Services.Identity.Application.Features.AdminUsers.Errors;

internal static class AdminUserErrors
{
    public const string ValidationErrorCode = "Identity.AdminUsers.Validation";

    public static Error NotFound(Guid id)
    {
        return Error.NotFound("Identity.AdminUsers.NotFound", $"Admin user '{id}' was not found.");
    }

    public static Error UserNameConflict()
    {
        return Error.Conflict("Identity.AdminUsers.UserNameConflict", "Admin user name already exists.");
    }

    public static Error EmailConflict()
    {
        return Error.Conflict("Identity.AdminUsers.EmailConflict", "Admin user email already exists.");
    }

    public static Error PhoneNumberConflict()
    {
        return Error.Conflict("Identity.AdminUsers.PhoneNumberConflict", "Admin user phone number already exists.");
    }

    public static Error RoleNotFound(Guid id)
    {
        return Error.NotFound("Identity.AdminUsers.RoleNotFound", $"Role '{id}' was not found.");
    }
}
