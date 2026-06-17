namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Conflicts;

public enum PersistenceConflictType
{
    Unknown = 0,
    UniqueConstraint = 1,
    Concurrency = 2
}
