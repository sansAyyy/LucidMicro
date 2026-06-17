namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Conflicts;

public interface IPersistenceConflictDetector
{
    bool TryDetect(Exception exception, out PersistenceConflict conflict);
}
