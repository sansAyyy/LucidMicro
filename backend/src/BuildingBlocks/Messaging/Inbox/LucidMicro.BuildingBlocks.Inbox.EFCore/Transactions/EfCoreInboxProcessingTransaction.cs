using LucidMicro.BuildingBlocks.Inbox.Core.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Inbox.EFCore.Transactions;

public sealed class EfCoreInboxProcessingTransaction<TDbContext> : IInboxProcessingTransaction
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public EfCoreInboxProcessingTransaction(TDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_dbContext.Database.CurrentTransaction is not null)
        {
            await operation(cancellationToken);
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
