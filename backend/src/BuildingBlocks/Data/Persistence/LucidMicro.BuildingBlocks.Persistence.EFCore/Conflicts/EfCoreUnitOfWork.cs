using LucidMicro.BuildingBlocks.Application.Exceptions;
using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Conflicts;
using LucidMicro.BuildingBlocks.Persistence.Abstractions.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Persistence.EFCore.Conflicts;

internal sealed class EfCoreUnitOfWork<TDbContext> : IUnitOfWork
    where TDbContext : DbContext
{
    private static readonly Error DefaultConcurrencyError = Error.Conflict(
        "Persistence.ConcurrencyConflict",
        "The data was changed by another operation. Please reload and try again.");

    private static readonly Error DefaultUniqueConstraintError = Error.Conflict(
        "Persistence.UniqueConstraintConflict",
        "A record with the same unique value already exists.");

    private readonly IPersistenceConflictDetector _conflictDetector;
    private readonly IEnumerable<IPersistenceConflictTranslator> _conflictTranslators;
    private readonly TDbContext _dbContext;

    public EfCoreUnitOfWork(
        TDbContext dbContext,
        IPersistenceConflictDetector conflictDetector,
        IEnumerable<IPersistenceConflictTranslator> conflictTranslators)
    {
        _dbContext = dbContext;
        _conflictDetector = conflictDetector;
        _conflictTranslators = conflictTranslators;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (_conflictDetector.TryDetect(exception, out var conflict))
        {
            throw new BusinessException(Translate(conflict));
        }
    }

    private Error Translate(PersistenceConflict conflict)
    {
        foreach (var translator in _conflictTranslators)
        {
            if (translator.TryTranslate(conflict, out var error))
            {
                return error;
            }
        }

        return conflict.Type switch
        {
            PersistenceConflictType.Concurrency => DefaultConcurrencyError,
            PersistenceConflictType.UniqueConstraint => DefaultUniqueConstraintError,
            _ => Error.Conflict("Persistence.Conflict", "The operation conflicts with existing data.")
        };
    }
}
