using LucidMicro.BuildingBlocks.EventBus.Abstractions.Models;
using LucidMicro.BuildingBlocks.Inbox.Abstractions.Contracts;
using LucidMicro.BuildingBlocks.Inbox.Abstractions.Models;
using LucidMicro.BuildingBlocks.Inbox.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.BuildingBlocks.Inbox.EFCore.Stores;

public sealed class EfCoreInboxMessageStore<TDbContext> : IInboxMessageStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfCoreInboxMessageStore(TDbContext dbContext)
        : this(dbContext, TimeProvider.System)
    {
    }

    public EfCoreInboxMessageStore(
        TDbContext dbContext,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<bool> HasProcessedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<InboxMessageEntity>()
            .FindAsync([id], cancellationToken) is not null;
    }

    public async Task MarkProcessedAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await _dbContext.Set<InboxMessageEntity>().AddAsync(
            InboxMessageEntity.FromMessage(new InboxMessage
            {
                Id = integrationEvent.Id,
                Type = IntegrationEventNameResolver.Resolve<TEvent>(),
                ProcessedAt = _timeProvider.GetUtcNow()
            }),
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
