using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Conflicts;

namespace LucidMicro.Services.Identity.Infrastructure.Persistence;

internal sealed class IdentityPersistenceConflictTranslator : IPersistenceConflictTranslator
{
    public bool TryTranslate(PersistenceConflict conflict, out Error error)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        error = conflict.ConstraintName switch
        {
            "ix_admin_users_user_name" => Error.Conflict(
                "Identity.AdminUsers.UserNameConflict",
                "Admin user name already exists."),
            "ix_admin_users_email" => Error.Conflict(
                "Identity.AdminUsers.EmailConflict",
                "Admin user email already exists."),
            "ix_admin_users_phone_number" => Error.Conflict(
                "Identity.AdminUsers.PhoneNumberConflict",
                "Admin user phone number already exists."),
            "ix_roles_code" => Error.Conflict(
                "Identity.Roles.CodeConflict",
                "Role code already exists."),
            _ => Error.None
        };

        return error != Error.None;
    }
}
