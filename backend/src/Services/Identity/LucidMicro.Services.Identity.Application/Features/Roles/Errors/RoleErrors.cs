using LucidMicro.BuildingBlocks.Application.Results;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Errors;

internal static class RoleErrors
{
    public const string ValidationErrorCode = "Identity.Roles.Validation";

    public static Error NotFound(Guid id)
    {
        return Error.NotFound("Identity.Roles.NotFound", $"Role '{id}' was not found.");
    }

    public static Error CodeConflict()
    {
        return Error.Conflict("Identity.Roles.CodeConflict", "Role code already exists.");
    }

    public static Error PermissionNotFound(Guid id)
    {
        return Error.NotFound("Identity.Roles.PermissionNotFound", $"Permission '{id}' was not found.");
    }
}
