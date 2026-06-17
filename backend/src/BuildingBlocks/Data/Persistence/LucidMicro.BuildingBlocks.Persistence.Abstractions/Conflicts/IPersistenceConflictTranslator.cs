using LucidMicro.BuildingBlocks.Application.Results;

namespace LucidMicro.BuildingBlocks.Persistence.Abstractions.Conflicts;

public interface IPersistenceConflictTranslator
{
    bool TryTranslate(PersistenceConflict conflict, out Error error);
}
