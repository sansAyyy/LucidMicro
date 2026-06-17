namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Conflicts;

public sealed record PersistenceConflict(
    PersistenceConflictType Type,
    string? ConstraintName = null);
