using LucidMicro.BuildingBlocks.Persistence.Abstractions.Conflicts;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Conflicts;

internal sealed class EfCorePersistenceConflictDetector : IPersistenceConflictDetector
{
    private const string PostgresUniqueViolation = "23505";
    private const int SqliteConstraint = 19;

    public bool TryDetect(Exception exception, out PersistenceConflict conflict)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is DbUpdateConcurrencyException)
        {
            conflict = new PersistenceConflict(PersistenceConflictType.Concurrency);
            return true;
        }

        if (exception is DbUpdateException updateException
            && TryDetectUniqueConstraint(updateException, out var constraintName))
        {
            conflict = new PersistenceConflict(PersistenceConflictType.UniqueConstraint, constraintName);
            return true;
        }

        conflict = default!;
        return false;
    }

    private static bool TryDetectUniqueConstraint(Exception exception, out string? constraintName)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.GetType().FullName == "Npgsql.PostgresException"
                && string.Equals(GetStringProperty(current, "SqlState"), PostgresUniqueViolation, StringComparison.Ordinal))
            {
                constraintName = GetStringProperty(current, "ConstraintName");
                return true;
            }

            if (current.GetType().FullName == "Microsoft.Data.Sqlite.SqliteException"
                && GetIntProperty(current, "SqliteErrorCode") == SqliteConstraint)
            {
                constraintName = null;
                return true;
            }
        }

        constraintName = null;
        return false;
    }

    private static string? GetStringProperty(object value, string propertyName)
    {
        return value.GetType().GetProperty(propertyName)?.GetValue(value) as string;
    }

    private static int? GetIntProperty(object value, string propertyName)
    {
        return value.GetType().GetProperty(propertyName)?.GetValue(value) as int?;
    }
}
